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
    [CalloutInfo("NoiseComplaint", CalloutProbability.High)]
    class NoiseComplaint : Callout
    {
        public ENoiseStatus status;
        private Ped Attacker;
        private Ped myPed2;
        private Blip myBlip;
        private LHandle pursuit;
        private bool isDead;

        //attacker          //PED
        float[] xCoods = new float[] { 1383.68457f, 1374.58862f };
        float[] yCoods = new float[] { -1607.56f, -1606.89392f };
        float[] zCoods = new float[] { 55.7976837f, 54.57649f };
        float[] headings = new float[] { 51.7f, 252.0f };

        //SPAWN POINT COORD SETTER!
        //***************************************************************************
        public struct SpawnPoint
        {
            public float Heading;
            public Vector3 Position;
            public SpawnPoint(float Heading, Vector3 Position)
            {
                this.Heading = Heading;
                this.Position = Position;
            }
            public static SpawnPoint Zero
            {
                get
                {
                    return new SpawnPoint(0.0f, Vector3.Zero);
                }
            }
        };
        //***************************************************************************



        public override bool OnBeforeCalloutDisplayed()
        {

            SpawnPoint sp = new SpawnPoint(headings[0], new Vector3(xCoods[0], yCoods[0], zCoods[0]));
            SpawnPoint sp2 = new SpawnPoint(headings[1], new Vector3(xCoods[1], yCoods[1], zCoods[1]));

            Attacker = new Ped(sp.Position);
            myPed2 = new Ped(sp2.Position);

            if (!Attacker.Exists()) return false;
            if (!myPed2.Exists()) return false;

            this.ShowCalloutAreaBlipBeforeAccepting(Attacker.Position, 65f);
            this.AddMinimumDistanceCheck(5f, Attacker.Position);

            this.CalloutMessage = "Shanking";
            this.CalloutPosition = Attacker.Position;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS SHOTS_FIRED IN_OR_ON_POSITION INTRO MULTIPLE_OFFICERS_DOWN CRIME_GUNFIRE", Attacker.Position);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            myBlip = Attacker.AttachBlip();
            myBlip.EnableRoute(Color.Red);

            //Attacker.BlockPermanentEvents = true;
            //myPed2.BlockPermanentEvents = true;

            Game.DisplaySubtitle("Head to the scene " + Main.playerName + "!", 5000);

            status = ENoiseStatus.EnRoute;

            //this.pursuit = Functions.CreatePursuit();
            //Functions.AddPedToPursuit(pursuit, Attacker);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (Attacker.Exists()) Attacker.Delete();
            if (myPed2.Exists()) myPed2.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        public override void Process()
        {
            base.Process();

            if (status == ENoiseStatus.EnRoute && Game.LocalPlayer.Character.Position.DistanceTo(Attacker.Position) <= 15)
            {
                status = ENoiseStatus.OnScene;

                StartMurderScenario();
            }


            if (status == ENoiseStatus.DecisionMade && !Functions.IsPursuitStillRunning(pursuit))
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
            if (Attacker.Exists()) Attacker.Dismiss();
            if (myPed2.Exists()) myPed2.Dismiss();
            if (myBlip.Exists()) myBlip.Delete();

        }

        public void StartMurderScenario()
        {
            GameFiber.StartNew(delegate
            {
                this.pursuit = Functions.CreatePursuit();

                int r = new Random().Next(1, 5);
                int t = new Random().Next(1, 3);

                if (r == 1)
                {
                    NativeFunction.CallByName<uint>("TASK_COMBAT_PED", Attacker, myPed2, 0, 1);
                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", myPed2, Attacker);

                    GameFiber.Sleep(5000);

                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", myPed2, Attacker);

                    if (t == 1)
                    {
                        NativeFunction.CallByName<uint>("TASK_COMBAT_PED", Attacker, Game.LocalPlayer.Character, 0, 1);

                        GameFiber.Sleep(4500);
                    }

                    if (t == 2)
                    {
                        myPed2.Kill();

                        if (!myPed2.IsAlive)
                        {
                            isDead = true;
                        }

                        if (isDead == true)
                        {
                            NativeFunction.CallByName<uint>("TASK_AIM_GUN_AT_ENTITY", Attacker, myPed2, -1, true);
                        }
                    }
                }
                else
                {
                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", myPed2, Attacker);
                }

                if (Attacker.Exists())
                {
                    Attacker.Dismiss();
                }

            });
        }

        public enum ENoiseStatus
        {
            EnRoute,
            OnScene,
            DecisionMade
        }
    }
}
