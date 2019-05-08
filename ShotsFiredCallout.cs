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

    [CalloutInfo("CrazyShooter", CalloutProbability.High)]
    class ShotsFiredCallout : Callout
    {
        private Ped myPed;
        private Ped player = Game.LocalPlayer.Character;
        private Vector3 SpawnPoint;
        private Blip myBlip;
        private LHandle pursuit;
        private Random myRand = new Random();

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

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f));

            myPed = new Ped(SpawnPoint);

            if (!myPed.Exists()) return false;

            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 65f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);

            // For The Suspect Ped
            myPed.RelationshipGroup = new RelationshipGroup("SUSPECT");
            Game.SetRelationshipBetweenRelationshipGroups("SUSPECT", "PLAYER", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("SUSPECT", "COP", Relationship.Hate);

            this.CalloutMessage = "Random Shots fired";
            this.CalloutPosition = SpawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS SHOTS_FIRED IN_OR_ON_POSITION INTRO MULTIPLE_OFFICERS_DOWN CRIME_GUNFIRE", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            string myPedsGun = pedsGun[myRand.Next(pedsGun.Length)];

            myPed.Inventory.GiveNewWeapon(myPedsGun, 50, true);

            myBlip = myPed.AttachBlip();
            // Make's a route line in GPS
            myBlip.EnableRoute(Color.Turquoise);

            this.pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(this.pursuit, myPed);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (myPed.Exists()) myPed.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        public override void Process()
        {
            base.Process();

            if (!myPed.IsInAnyVehicle(false) && myPed.Exists() && !Functions.IsPedGettingArrested(myPed) && !Functions.IsPedArrested(myPed))
            {
                NativeFunction.CallByName<uint>("TASK_COMBAT_PED", myPed, player, 0, 1);
            }

            if (!Functions.IsPursuitStillRunning(pursuit))
            {
                if (myPed.Exists() || myPed.IsAlive)
                {
                    Functions.RequestBackup(myPed.Position.Around(25f), LSPD_First_Response.EBackupResponseType.SuspectTransporter, LSPD_First_Response.EBackupUnitType.PrisonerTransport);
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
            if (myPed.Exists()) myPed.Dismiss();
        }
    }
}