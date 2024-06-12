using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatShared;
using Comfort.Logs;
using EFT;
using EFT.HandBook;
using EFT.Hideout;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using HallOfFameImprovements.Patches;

namespace HallOfFameImprovements
{
    [BepInPlugin("com.utjan.HoFImprovements", "utjan.HoFImprovements", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        internal static ConfigEntry<bool> enabledPlugin;
        internal static ConfigEntry<double> bonusMultiplierNonDogtagItems;
        internal static ConfigEntry<double> bonusMultiplierDogtags;
        internal static ConfigEntry<double> uniqueItemBonus;

        private void Awake() //Awake() will run once when your plugin loads
        {
            enabledPlugin = Config.Bind(
                "Hall of Fame Improvements",
                "1. Enable Mod",
                true,
                new ConfigDescription("Enable Hall of Fame Improvements")
            );

            bonusMultiplierNonDogtagItems = Config.Bind(
                "Hall of Fame Improvements",
                "Trophy skill bonus multiplier",
                1d,
                new ConfigDescription("Multiplies the base bonus to Hall of Fame skill buff gained from non-dogtag items. 0.5 = half bonus gain. 2.0 = double bonus gain", new AcceptableValueRange<double>(0, 5))
            );

            bonusMultiplierDogtags = Config.Bind(
                "Hall of Fame Improvements",
                "Dogtag skill bonus multiplier",
                2d,
                new ConfigDescription("Multiplies the base bonus to Hall of Fame skill buff gained from dogtags. Leave at 1 for default EFT bonus", new AcceptableValueRange<double>(0, 5))
            );

            uniqueItemBonus = Config.Bind(
                "Hall of Fame Improvements",
                "Buff bonus per unique item",
                0.2d,
                new ConfigDescription("Hall of Fame skill buff bonus gained per unique non-dogtag item. Applies on top of the bonus gained based on the item's value", new AcceptableValueRange<double>(0, 1))
            );

            LogSource = Logger;

            new ApplyNonDogtagItemsPatch().Enable();
            new UpdateBonusOnItemSlottedPatch().Enable();
        }
    }
}
