using MelonLoader;
using UnityEngine;
using System.Globalization;
using static BlindRumble2.Core;

namespace BlindRumble2
{
    public class UISetup
    {
        internal const string USER_DATA = "UserData/BlindRumble/";
        internal const string CONFIG_FILE = "config.cfg";

        internal static MelonPreferences_Category category1;
        internal static MelonPreferences_Category category2;
        internal static MelonPreferences_Entry<bool> enabledMod;
        internal static MelonPreferences_Entry<bool> enableInGym;
        internal static MelonPreferences_Entry<bool> enableInPark;
        internal static MelonPreferences_Entry<bool> enableInMatch;
        internal static MelonPreferences_Entry<string> MainColor;
        internal static MelonPreferences_Entry<string> SecondaryColor;

        public static void LoadPrefs()
        {
            if (!Directory.Exists(USER_DATA))
            {
                Directory.CreateDirectory(USER_DATA);
            }
                

            category1 = MelonPreferences.CreateCategory("Main");
            category1.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

            category2 = MelonPreferences.CreateCategory("Colors");
            category2.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

            enabledMod = category1.CreateEntry("enabledMod", true, "Enable Mod", "Enables the mod. Other settings wont work if this is disabled.");
            enableInGym = category1.CreateEntry("enableInGym", false, "Enable In Gym", "Enables Blind Rumble within Gym. Defaults to false.", !enabledMod.Value);
            enableInPark = category1.CreateEntry("enableInPark", true, "Enable In Park", "Enables Blind Rumble within Park. Defaults to true.", !enabledMod.Value);
            enableInMatch = category1.CreateEntry("enableInMatch", true, "Enable In Match", "Enables Blind Rumble within a match. Defaults to true.", !enabledMod.Value);
            MainColor = category2.CreateEntry("MainColor", "#fed44a", "Main Color", "Color used for structures and players. Hex format (ex. #ffffff).", !enabledMod.Value);
            SecondaryColor = category2.CreateEntry("SecondaryColor", "#31966b", "Secondary Color", "Color used for scene stuff. Hex format.", !enabledMod.Value);
        }

        public static void SetPrefs()
        {
            modEnabled = enabledMod.Value;
            EIGym = enableInGym.Value;
            EIPark = enableInPark.Value;
            EIMatch = enableInMatch.Value;
            if (!HexToColor(MainColor.Value, out MainSonar)) loggerInstance.Error("Main Color did not save!\nCauses are improper syntax or wrong format.");
            if (!HexToColor(SecondaryColor.Value, out SecondarySonar)) loggerInstance.Error("Secondary Color did not save!\nCauses are improper syntax or wrong format.");

            if (CurrentSceneName != "Loader") MelonCoroutines.Start(SonarifyScene());
        }

        private static bool HexToColor(string hex, out Color color)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6)
            {
                color = Color.white;
                return false;
            }

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            color = new Color32(r, g, b, 255);
            return true;
        }
    }
}
