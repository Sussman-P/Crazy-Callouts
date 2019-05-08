using System.Windows;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage.Native;
using Rage;

namespace CrazyCallouts.Callouts
{
    [CalloutInfo("Murder", CalloutProbability.High)]
    class MurderCallout : Callout
    {
        public EMurderState state;
        public bool isDead;
        private Ped Aggressor;
        private Ped Victim;
        private Ped player = Game.LocalPlayer.Character;
        private Vector3 SpawnPoint;
        private Blip aggressorBlip;
        private LHandle pursuit;

        private string[] pedsGun = { "WEAPON_ADVANCEDRIFLE", 
                                     "WEAPON_PUMPSHOTGUN" ,
                                     "WEAPON_COMBATMG",
                                     "WEAPON_MG",
                                     "WEAPON_CARBINERIFLE",
                                     "WEAPON_ASSAULTRIFLE",
                                     "WEAPON_PISTOL50",
                                     "WEAPON_APPISTOL",
                                     "WEAPON_HEAVYSNIPER",
                                     "WEAPON_MICROSMG",
                                     "WEAPON_SMG" };

        Random myRand = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f));

            Aggressor = new Ped(SpawnPoint);
            Victim = new Ped(SpawnPoint.Around(1.5f));

            if (!Aggressor.Exists()) return false;
            if (!Victim.Exists()) return false;

            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 80f);
            this.AddMinimumDistanceCheck(5f, Aggressor.Position);

            this.CalloutMessage = "Psycho with Gun";
            this.CalloutPosition = SpawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS UNITS_REPORTING CRIME_HOMOCIDE IN_OR_ON_POSITION INTRO CRIME_GUNFIRE", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            state = EMurderState.EnRoute;

            string myPedsGun = pedsGun[myRand.Next(pedsGun.Length)];

            aggressorBlip = Aggressor.AttachBlip();
            aggressorBlip.EnableRoute(Color.DarkOrange);

            Aggressor.Inventory.GiveNewWeapon(myPedsGun, 50, true);

            if (Victim.IsAlive)
            {
                NativeFunction.CallByName<uint>("TASK_AIM_GUN_AT_ENTITY", Aggressor, Victim, -1, true);
                Victim.Tasks.PutHandsUp(-1, Aggressor);

                Victim.BlockPermanentEvents = true;
            }

            Game.DisplaySubtitle("~b~" + Main.playerName + " ~w~Get to the ~r~scene~w~.", 6500);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (Aggressor.Exists()) Aggressor.Delete();
            if (Victim.Exists()) Victim.Delete();
            if (aggressorBlip.Exists()) aggressorBlip.Delete();
        }

        public override void Process()
        {
            base.Process();

            if (state == EMurderState.EnRoute && Game.LocalPlayer.Character.Position.DistanceTo(SpawnPoint) <= 15)
            {
                state = EMurderState.OnScene;

                StartMurderScenario();
            }

            if (state == EMurderState.DecisionMade && !Functions.IsPursuitStillRunning(pursuit))
            {
                this.End();
            }

            if (Game.IsKeyDown(Keys.End))
            {
                this.End();
            }
        }

        public override void End()
        {
            base.End();
            if (Aggressor.Exists()) Aggressor.Dismiss();
            if (Victim.Exists()) Victim.Dismiss();
            if (aggressorBlip.Exists()) aggressorBlip.Delete();
        }

        public void StartMurderScenario()
        {
            GameFiber.StartNew(delegate
            {
                this.pursuit = Functions.CreatePursuit();

                int r = new Random().Next(1, 5);
                int t = new Random().Next(1, 3);

                state = EMurderState.DecisionMade;

                if (r == 1)
                {
                    NativeFunction.CallByName<uint>("TASK_COMBAT_PED", Aggressor, Victim, 0, 1);
                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", Victim, Aggressor);

                    GameFiber.Sleep(5000);

                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", Victim, Aggressor);

                    if (t == 1)
                    {
                        NativeFunction.CallByName<uint>("TASK_COMBAT_PED", Aggressor, Game.LocalPlayer.Character, 0, 1);

                        GameFiber.Sleep(4500);
                    }

                    if (t == 2)
                    {
                        Victim.Kill();

                        if (!Victim.IsAlive)
                        {
                            isDead = true;
                        }

                        if (isDead == true)
                        {
                            NativeFunction.CallByName<uint>("TASK_AIM_GUN_AT_ENTITY", Aggressor, Victim, -1, true);
                        }
                    }
                }
                else
                {
                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", Victim, Aggressor);
                }

                if (Aggressor.Exists())
                {
                    Aggressor.Dismiss();
                }

                Functions.AddPedToPursuit(this.pursuit, Aggressor);

                Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
            });
        }

        public enum EMurderState
        {
            EnRoute,
            OnScene,
            DecisionMade
        }
    }
}
