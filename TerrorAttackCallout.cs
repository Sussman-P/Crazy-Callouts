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

    [CalloutInfo("TerrorAttack", CalloutProbability.Medium)]
    class TerrorAttackCallout : Callout
    {
        private Ped myPed;
        private Ped player = Game.LocalPlayer.Character;
        private List<Ped> pedsList = new List<Ped>();
        private Vector3 SpawnPoint;
        private LHandle pursuit;

        private string[] pedsGun = { //"WEAPON_ADVANCEDRIFLE", 
                                     //"WEAPON_PUMPSHOTGUN" ,
                                     //"WEAPON_COMBATMG",
                                     //"WEAPON_MG",
                                    "WEAPON_CARBINERIFLE",
                                     //"WEAPON_ASSAULTRIFLE",
                                     //"WEAPON_PISTOL50",
                                     //"WEAPON_APPISTOL",
                                     //"WEAPON_HEAVYSNIPER",
                                     //"WEAPON_MICROSMG",
                                     //"WEAPON_SMG",
                                     "WEAPON_RPG" };

        private string[] relationGroups = { "PLAYER", 
                                              "COP", 
                                              "CIVMALE", 
                                              "CIVFEMALE", 
                                              "AMBIENT_GANG_LOST", 
                                              "AMBIENT_GANG_MEXICAN", 
                                              "AMBIENT_GANG_FAMILY", 
                                              "AMBIENT_GANG_BALLAS", 
                                              "AMBIENT_GANG_MARABUNTE", 
                                              "AMBIENT_GANG_CULT", 
                                              "AMBIENT_GANG_SALVA", 
                                              "AMBIENT_GANG_WEICHENG", 
                                              "AMBIENT_GANG_HILLBILLY", 
                                              "FIREMAN", 
                                              "MEDIC"};

        private Random myRand = new Random();
        // This is for when before the callout has been initialized/displayed
        public override bool OnBeforeCalloutDisplayed()
        {
            int x = myRand.Next(1, 6);

            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(300f));

            for (int i = 0; i < x; i++)
            {
                myPed = new Ped("s_m_m_movalien_01", SpawnPoint.Around(5f), 0f);
                pedsList.Add(myPed);

                // When Spawned, They automatically start shooting (WHICH ISN'T GOOD)
                NativeFunction.CallByName<uint>("SET_PED_COMBAT_ATTRIBUTES", myPed, 46/*BF_AlwaysFight*/, true);
            }

            foreach (Ped peds in pedsList)
            {
                if (!peds.Exists())
                {
                    return false;
                }

                if (peds.Exists())
                {
                    string myPedsGun = pedsGun[myRand.Next(pedsGun.Length)];
                    peds.Inventory.GiveNewWeapon(myPedsGun, 500, true);
                }

                foreach (string myRelationGroups in relationGroups)
                {
                    peds.RelationshipGroup = new RelationshipGroup("Terrorist");
                    Game.SetRelationshipBetweenRelationshipGroups("Terrorist", "PLAYER", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("Terrorist", "COP", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("Terrorist", "CIVMALE", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("Terrorist", "CIVFEMALE", Relationship.Hate);
                }
            }

            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 80f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);

            this.CalloutMessage = "Terrorist Activity";
            this.CalloutPosition = SpawnPoint;

            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_TERRORIST_ACTIVITY IN_OR_ON_POSITION INTRO MULTIPLE_OFFICERS_DOWN CRIME_GUNFIRE UNITS_RESPOND_CODE_99", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            this.pursuit = Functions.CreatePursuit();

            foreach (Ped peds2 in pedsList)
            {
                Functions.AddPedToPursuit(this.pursuit, peds2);
            }

            Functions.RequestBackup(player.Position.Around(25f), LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            foreach (Ped deletePed in pedsList)
            {
                if (deletePed.Exists()) deletePed.Delete();
            }
        }

        public override void Process()
        {
            base.Process();

            foreach (Ped pedsInGame in pedsList)
            {
                if (!pedsInGame.IsInAnyVehicle(false) && pedsInGame.Exists() && !Functions.IsPedGettingArrested(pedsInGame) && !Functions.IsPedArrested(pedsInGame))
                {
                    NativeFunction.CallByName<uint>("TASK_COMBAT_PED", pedsInGame, player, 0, 1);
                }
            }

            if (!Functions.IsPursuitStillRunning(pursuit))
            {
                foreach (Ped pedsAlive in pedsList)
                {
                    if (pedsAlive.Exists() || pedsAlive.IsAlive)
                    {
                        this.End();
                    }
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
            foreach (Ped dismissPed in pedsList)
            {
                if (dismissPed.Exists()) dismissPed.Dismiss();
            }
        }
    }
}