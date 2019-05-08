using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CrazyCallouts.Callouts;

namespace CrazyCallouts
{
    using LSPD_First_Response.Mod.API;
    using Rage;

    public class Main : Plugin
    {
        public static InitializationFile setting = new InitializationFile(@"Plugins\LSPDFR\CrazyCallouts.ini");

        public static string playerName = setting.ReadString("Main", "YourName", "NoNameSet");

        public Main()
        {
        }

        public override void Finally()
        {
        }

        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Game.LogTrivial("CrazyCalloutsV0.5.8 Plugin loaded!");
        }

        static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                // Shows In the Bottom left corner when we get on duty!
                Game.DisplayNotification("CrazyCalloutsV0.5.8 Plugin loaded!");

                //TO BE FIXED
                //Functions.RegisterCallout(typeof(NoiseComplaint));
                //

                Functions.RegisterCallout(typeof(CrazyDriverCallout));
                Functions.RegisterCallout(typeof(ShotsFiredCallout));
                Functions.RegisterCallout(typeof(MurderCallout));
                Functions.RegisterCallout(typeof(KidnapCallout));

                // Terrorist Callout
                Functions.RegisterCallout(typeof(TerrorAttackCallout));
            }
        }
    }
}
