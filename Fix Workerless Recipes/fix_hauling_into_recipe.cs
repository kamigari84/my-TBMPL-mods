using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using UnityEngine;
//using UnityEngine.MonoBehaviour;
//using UnityEngine.CoreModule;
using Timberborn.Common;
using Timberborn.InventorySystem;
using Timberborn.Goods;
using System.Reflection;

namespace WorkerlessRecipe_HaulingFix
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "Hauling22RecipeFix", "Fixing hauling priority dropping off way too early", "1.0.3")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig();
        }

    }
    internal sealed class EPConfig : BaseConfig { }


    // Some of our patches for the game
    [HarmonyPatch(typeof(InventoryFillCalculator))]
    internal static class Patches
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(InventoryFillCalculator), "GetInventoryFillPercentage");
        }
        static void Prefix(Inventory inventory, ReadOnlyHashSet<string> goods, bool onlyInStock, ref float __result)
        {
            float fill = 0f; // keep track of the average filling of all ingredients
            ReadOnlyList<float> fills = new ReadOnlyList<float>();
            fills.AddItem(fill);
            foreach (StorableGoodAmount allowedGood in inventory.AllowedGoods)
            {
                string goodId = allowedGood.StorableGood.GoodId;
                if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
                {
                    int num3 = inventory.AmountInStock(goodId);
                    if (!onlyInStock || num3 > 0)
                    {
                        fills.AddItem(Mathf.Clamp01((float)num3 / (float)allowedGood.Amount)); // return highest fullness ratio of the various output goods
                    }
                }
            }
            __result = Mathf.Max(fill);
        }
        [HarmonyPatch("GetInputFillPercentage")]
        static void Prefix(Inventory inventory, ref float __result)        {
            float fill = 1f; // keep track of the average filling of all ingredients
            ReadOnlyList<float> fills = new ReadOnlyList<float>();
            fills.AddItem(fill);
            ReadOnlyHashSet<string> goods = inventory.InputGoods;
            foreach (StorableGoodAmount allowedGood in inventory.AllowedGoods)
            {
                string goodId = allowedGood.StorableGood.GoodId;
                if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
                {
                    int num3 = inventory.AmountInStock(goodId);
                    fills.AddItem(Mathf.Clamp01((float)num3 / (float)allowedGood.Amount));
                }
            }
            __result = Mathf.Min(fill); // return lowest fullness ratio of the various input goods
        }
    }
}