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
                "Non-Dogtag Item buff bonus multiplier",
                1d,
                new ConfigDescription("Multiplies the base bonus to Hall of Fame skill buff gained from non-dogtag items. 0.5 = half bonus gain. 2.0 = double bonus gain", new AcceptableValueRange<double>(0, 5))
            );

            bonusMultiplierDogtags = Config.Bind(
                "Hall of Fame Improvements",
                "Dogtag buff bonus multiplier",
                1d,
                new ConfigDescription("Multiplies the base bonus to Hall of Fame skill buff gained from dogtags. Leave at 1 for default EFT bonus", new AcceptableValueRange<double>(0, 5))
            );

            uniqueItemBonus = Config.Bind(
                "Hall of Fame Improvements",
                "Buff bonus per unique item",
                0.1d,
                new ConfigDescription("Hall of Fame skill buff bonus gained per unique non-dogtag item. Applies on top of the bonus gained based on the item's value", new AcceptableValueRange<double>(0, 1))
            );

            LogSource = Logger;

            new ApplyNonDogtagItemsPatch().Enable();
            new UpdateBonusOnItemSlottedPatch().Enable();
        }
    }

    internal class ApplyNonDogtagItemsPatch : ModulePatch
    {
        public static Dictionary<string, HandbookData> hbData;

        public static List<string> uniqueItemList;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlaceOfFameBehaviour), nameof(PlaceOfFameBehaviour.method_14));
        }

        [PatchPostfix]
        static void Postfix(PlaceOfFameBehaviour __instance)
        {
            if (!Plugin.enabledPlugin.Value)
                return;

#if DEBUG
            Plugin.LogSource.LogWarning($"Calculating Hall of Fame bonus");
#endif

            GClass1420 gclass;
            if ((gclass = (__instance.Data.CurrentStage.Bonuses.Data.FirstOrDefault(new Func<GClass1407, bool>(PlaceOfFameBehaviour.Class1644.class1644_0.method_3)) as GClass1420)) != null)
            {
                double double_0 = Traverse.Create(__instance).Field("double_0").GetValue<double>();

                double_0 *= Plugin.bonusMultiplierDogtags.Value;

#if DEBUG
                Plugin.LogSource.LogWarning($"Bonus from dogtags {double_0}");
#endif

                uniqueItemList = new List<string>();

                LootItemClass gclass2629_0 = Traverse.Create(__instance).Field("gclass2629_0").GetValue<LootItemClass>();
                using (List<Item>.Enumerator enumerator = gclass2629_0.GetAllItems().Where(new Func<Item, bool>(ItemIsNotDogtag)).ToList<Item>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        float price = GetItemHandbookPrice(enumerator.Current);
                        if(price <= 0)
                            continue;

                        double bonus = GetBuffBonusFromPrice(price);

                        if (Plugin.uniqueItemBonus.Value > 0 && !uniqueItemList.Contains(enumerator.Current.TemplateId))
                        {
                            bonus += Plugin.uniqueItemBonus.Value;
                            uniqueItemList.Add(enumerator.Current.TemplateId);
                        }

                        if (bonus == 0)
                            continue;

#if DEBUG
                        Plugin.LogSource.LogWarning($"BONUS of {enumerator.Current.Name.Localized()} is {bonus}");
#endif

                        double_0 += bonus * Plugin.bonusMultiplierNonDogtagItems.Value;
                    }
                }

                BonusController bonusController_0 = Traverse.Create(__instance).Field("bonusController_0").GetValue<BonusController>();

                GClass1420 gclass2 = new GClass1420(Math.Round(double_0, 1), gclass.BoostValue, gclass.Id, gclass.IsVisible, gclass.Icon);
                bonusController_0.RemoveBonus(gclass, false);
                __instance.Data.CurrentStage.Bonuses.Data.Remove(gclass);
                __instance.Data.CurrentStage.Bonuses.Data.Add(gclass2);
                bonusController_0.AddBonus(gclass2, false);

#if DEBUG
                Plugin.LogSource.LogWarning($"HALL OF FAME BONUS UPDATED: {double_0}");
#endif
            }
        }
        public static bool ItemIsNotDogtag(Item i)
        {
            return i.GetItemComponent<DogtagComponent>() == null;
        }

        public static float GetItemHandbookPrice(Item lootItem)
        {
            if (hbData == null)
            {
                hbData = Singleton<HandbookClass>.Instance.Items.ToDictionary(
                    (item) => item.Id
                );
            }

            hbData.TryGetValue(lootItem.TemplateId, out HandbookData value);
            float price = value?.Price ?? 0;

#if DEBUG
            Plugin.LogSource.LogWarning($"Price of {lootItem.Name.Localized()} is {price}");
#endif

            return price;
        }

        public static double GetBuffBonusFromPrice(float price)
        {
            if (price <= 0)
                return 0;
            double bonus = 0;
            bonus = (Math.Log10(price) - 4) / 8;
            bonus = Math.Max(bonus, 0);
            return bonus;
        }
    }

    internal class UpdateBonusOnItemSlottedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlaceOfFameBehaviour), nameof(PlaceOfFameBehaviour.method_12));
        }

        [PatchPostfix]
        static void Postfix(PlaceOfFameBehaviour __instance, Item item, EFT.InventoryLogic.IContainer itemContainer, CommandStatus status)
        {
            if (!Plugin.enabledPlugin.Value)
                return;

            if (status != CommandStatus.Succeed)
            {
                return;
            }
            if (item.GetItemComponent<DogtagComponent>() == null && __instance.method_13(itemContainer))
            {
                __instance.method_14();
            }
        }
    }
}
