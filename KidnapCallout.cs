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

    [CalloutInfo("KidnapCallout", CalloutProbability.High)]
    class KidnapCallout : Callout
    {
        private Vehicle myVehicle;
        private Ped takerPed;
        private Ped kidnappedPed;
        private Ped player = Game.LocalPlayer.Character;
        private Vector3 SpawnPoint;
        private Blip myBlip;
        private LHandle pursuit;

        private string[] vehModels = { "Felon", "Ingot", "Premier", "Stanier" };

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
            string myVehModel = vehModels[myRand.Next(vehModels.Length)];

            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f));

            myVehicle = new Vehicle(myVehModel, SpawnPoint);
            takerPed = new Ped(SpawnPoint);
            kidnappedPed = new Ped(SpawnPoint);

            if (!myVehicle.Exists()) return false;
            if (!takerPed.Exists()) return false;
            if (!kidnappedPed.Exists()) return false;

            takerPed.WarpIntoVehicle(myVehicle, -1);
            kidnappedPed.WarpIntoVehicle(myVehicle, 2);

            // For The Taker Ped
            takerPed.RelationshipGroup = new RelationshipGroup("TAKER");
            Game.SetRelationshipBetweenRelationshipGroups("TAKER", "PLAYER", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("TAKER", "COP", Relationship.Hate);


            // For The Kidnapped Ped
            kidnappedPed.RelationshipGroup = new RelationshipGroup("victim");
            Game.SetRelationshipBetweenRelationshipGroups("victim","PLAYER",Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("victim", "COP", Relationship.Companion);

            kidnappedPed.BlockPermanentEvents = true;

            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 45f);
            this.AddMinimumDistanceCheck(5f, takerPed.Position);

            this.CalloutMessage = "Kidnapping In Progress";
            this.CalloutPosition = SpawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_KIDNAPPING IN_OR_ON_POSITION INTRO CRIME_GUNFIRE", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            string myPedsGun = pedsGun[myRand.Next(pedsGun.Length)];

            myBlip = takerPed.AttachBlip();
            myBlip.Order = 100;
            myBlip.Color = Color.HotPink;

            takerPed.Inventory.GiveNewWeapon(myPedsGun, 100, true);

            this.pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(this.pursuit, takerPed);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (myVehicle.Exists()) myVehicle.Delete();
            if (takerPed.Exists()) takerPed.Delete();
            if (kidnappedPed.Exists()) kidnappedPed.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        public override void Process()
        {
            base.Process();
            //Test Code
            if (!takerPed.IsInAnyVehicle(false) && takerPed.Exists() && !Functions.IsPedGettingArrested(takerPed) && !Functions.IsPedArrested(takerPed))
            {
                NativeFunction.CallByName<uint>("TASK_COMBAT_PED", takerPed, player, 0, 1);
            }

            if (Vector3.Distance(player.Position, kidnappedPed.Position) <= 10.0f && !takerPed.IsInAnyVehicle(false))
            {
                if (takerPed.Exists())
                {
                    Game.DisplayHelp("To Release Victim, Press: ~b~Y");
                }

                if (Game.IsKeyDown(Keys.Y))
                {
                    if (kidnappedPed.Exists() && myVehicle.Exists())
                    {
                        Game.DisplaySubtitle("Thank you ~b~" + Main.playerName, 5000);
                        kidnappedPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();
                        kidnappedPed.Tasks.Wander().WaitForCompletion(10000);
                        this.End();
                    }

                    //kidnappedPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();
                    //kidnappedPed.Tasks.Wander().WaitForCompletion(10000);
                    //this.End();
                }
            }

            //Trial code
            if (Game.IsKeyDown(Keys.Y))
            {
                Game.DisplaySubtitle("Thank you " + Main.playerName, 5000);
            }

            //if (myPed.IsDead || !myPed.IsAlive || myPed.IsInAnyVehicle(false) && !myPed.IsInAnyVehicle(false) && Vector3.Distance(player.Position, kidnappedPed.Position) <= 10.0f)
            //{
            //    Game.DisplayHelp("To Release Victim, Press: ~b~Y");

            //    if (Game.IsKeyDown(Keys.Y))
            //    {
            //        Game.DisplaySubtitle("Thank you " + Main.playerName, 5000);

            //        kidnappedPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
            //        kidnappedPed.Tasks.Clear();
            //        kidnappedPed.Tasks.Wander();
            //        kidnappedPed.Dismiss();
            //        this.End();
            //    }
            //}

            if (!Functions.IsPursuitStillRunning(pursuit))
            {
                if (takerPed.Exists() || takerPed.IsAlive)
                {
                    this.End();
                }
            }

            if (Game.IsKeyDown(Keys.End))
            {
                this.End();
            }
        }

        public override void End()
        {
            base.End();
            if (myBlip.Exists()) myBlip.Delete();
            if (takerPed.Exists()) takerPed.Dismiss();
            if (myVehicle.Exists()) myVehicle.Dismiss();
            if (kidnappedPed.Exists()) kidnappedPed.Dismiss();
        }
    }
}