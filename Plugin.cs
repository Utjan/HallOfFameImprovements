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

        private void Awake() //Awake() will run once when your plugin loads
        {
            //enabledPlugin = Config.Bind(
            //    "Main Settings",
            //    "Enable Mod",
            //    true,
            //    new ConfigDescription("Enable timer multipliers")
            //);

            //timeMultiplierRepair = Config.Bind(
            //    "Main Settings",
            //    "Repair objective Time Multiplier",
            //    0.5f,
            //    new ConfigDescription("Multiplies the duration when doing 'Repairing objective' task action. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            //);

            //timeMultiplierHide = Config.Bind(
            //    "Main Settings",
            //    "Hide objective Time Multiplier",
            //    0.5f,
            //    new ConfigDescription("Multiplies the duration when doing 'Hiding objective' task action. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            //);

            //timeMultiplierProtect = Config.Bind(
            //    "Main Settings",
            //    "Protect objective Time Multiplier",
            //    0.5f,
            //    new ConfigDescription("Multiplies the time it takes to protect task objective. Like when placing a MS2000 marker. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            //);

            LogSource = Logger;

            LogSource.LogWarning($"AWAKE");

            new ApplyNonDogtagItemsPatch().Enable();
            new UpdateBonusOnItemSlottedPatch().Enable();
            
            LogSource.LogWarning($"HOF PATCHED");
            //new BeaconPlantPatch().Enable();
        }

    }

    internal class ApplyNonDogtagItemsPatch : ModulePatch
    {
        public static Dictionary<string, HandbookData> hbData;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlaceOfFameBehaviour), nameof(PlaceOfFameBehaviour.method_14));
        }

        [PatchPostfix]
        static void Postfix(PlaceOfFameBehaviour __instance)
        {
            Plugin.LogSource.LogWarning($"Calculating Hall of Fame bonus");

            GClass1420 gclass;
            if ((gclass = (__instance.Data.CurrentStage.Bonuses.Data.FirstOrDefault(new Func<GClass1407, bool>(PlaceOfFameBehaviour.Class1644.class1644_0.method_3)) as GClass1420)) != null)
            {
                double double_0 = Traverse.Create(__instance).Field("double_0").GetValue<double>();

                Plugin.LogSource.LogWarning($"Bonus from dogtags {double_0}");

                LootItemClass gclass2629_0 = Traverse.Create(__instance).Field("gclass2629_0").GetValue<LootItemClass>();

                using (List<Item>.Enumerator enumerator = gclass2629_0.GetAllItems().Where(new Func<Item, bool>(ItemIsNotDogtag)).ToList<Item>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        float price = GetItemHandbookPrice(enumerator.Current);
                        if(price <= 0)
                            continue;
                        double bonus = GetBuffBonusFromPrice(price);
                        if (bonus == 0)
                            continue;
                        Plugin.LogSource.LogWarning($"BONUS of {enumerator.Current.Name.Localized()} is {bonus}");
                        double_0 += bonus;
                    }
                }

                BonusController bonusController_0 = Traverse.Create(__instance).Field("bonusController_0").GetValue<BonusController>();

                GClass1420 gclass2 = new GClass1420(Math.Round(double_0, 1), gclass.BoostValue, gclass.Id, gclass.IsVisible, gclass.Icon);
                bonusController_0.RemoveBonus(gclass, false);
                __instance.Data.CurrentStage.Bonuses.Data.Remove(gclass);
                __instance.Data.CurrentStage.Bonuses.Data.Add(gclass2);
                bonusController_0.AddBonus(gclass2, false);

                Plugin.LogSource.LogWarning($"HALL OF FAME BONUS UPDATED");
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

            Plugin.LogSource.LogWarning($"Price of {lootItem.Name.Localized()} is {price}");

            return price;
        }

        public static double GetBuffBonusFromPrice(float price)
        {
            if (price <= 0)
                return 0;
            double bonus = 0;
            bonus = (Math.Log10(price) - 4) / 10;
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
