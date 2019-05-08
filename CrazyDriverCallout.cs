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

    [CalloutInfo("CrazyDriver", CalloutProbability.Medium)]
    class CrazyDriverCallout : Callout
    {
        private Vehicle myVehicle;
        private Ped player = Game.LocalPlayer.Character;
        private Ped myPed;
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

            SpawnPoint = World.GetNextPositionOnStreet(player.Position.Around(300f));

            myVehicle = new Vehicle(myVehModel, SpawnPoint);
            myPed = new Ped(SpawnPoint);

            if (!myVehicle.Exists()) return false;
            if (!myPed.Exists()) return false;

            myPed.WarpIntoVehicle(myVehicle, -1);

            // For The Runner Ped
            myPed.RelationshipGroup = new RelationshipGroup("RUNNER");
            Game.SetRelationshipBetweenRelationshipGroups("RUNNER", "PLAYER", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("RUNNER", "COP", Relationship.Hate);

            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 45f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);

            this.CalloutMessage = "Armed Driver";
            this.CalloutPosition = SpawnPoint;

            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_RESIST_ARREST IN_OR_ON_POSITION INTRO CRIME_GUNFIRE", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            myBlip = myPed.AttachBlip();

            string myPedsGun = pedsGun[myRand.Next(pedsGun.Length)];

            myPed.Inventory.GiveNewWeapon(myPedsGun, 100, true);

            this.pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(this.pursuit, myPed);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (myVehicle.Exists()) myVehicle.Delete();
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
            if (myVehicle.Exists()) myVehicle.Dismiss();
        }
    }
}