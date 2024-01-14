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
using TBMPLCore.Plugin.Logs;
using System.Linq;
using System.Collections.Generic;

namespace WorkerlessRecipe_HaulingFix
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "Hauling22RecipeFix", "Fixing hauling priority dropping off way too early", "1.0.4")]
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
    [HarmonyPatch]
    internal static class Patch1
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(InventoryFillCalculator), "GetInventoryFillPercentage");
        }
        static bool Prefix(Inventory inventory, ReadOnlyHashSet<string> goods, bool onlyInStock, ref float __result)
        {
            float fill = 0f; // keep track of the average filling of all ingredients
            List<float> fills = new List<float>
            {
                fill
            };
            foreach (StorableGoodAmount allowedGood in inventory.AllowedGoods)
            {
                string goodId = allowedGood.StorableGood.GoodId;
                if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
                {
                    int num3 = inventory.AmountInStock(goodId);
                    if (!onlyInStock || num3 > 0)
                    {
                        fills.Add(Mathf.Clamp01((float)num3 / (float)allowedGood.Amount)); // list all ratios
                    }
                }
            }
            __result = fills.Max<float>();
            Log.Info("Fullness of" + __result * 100f + " collated from" + fills);
            return false;
        }
    }
    [HarmonyPatch]
    internal static class Patch2
    {
        [HarmonyPatch(typeof(InventoryFillCalculator), "GetInputFillPercentage")]
        static bool Prefix(Inventory inventory, ref float __result)        {
            float fill = 1f; // keep track of the average filling of all ingredients
            List<float> fills = new List<float>
            {
                fill
            };
            ReadOnlyHashSet<string> goods = inventory.InputGoods;
            foreach (StorableGoodAmount allowedGood in inventory.AllowedGoods)
            {
                string goodId = allowedGood.StorableGood.GoodId;
                if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
                {
                    int num3 = inventory.AmountInStock(goodId);
                    fills.Add(Mathf.Clamp01((float)num3 / (float)allowedGood.Amount)); //list all ratios
                }
            }
            __result = fills.Min<float>();
            Log.Info("Fullness of"+__result*100f+" collated from"+fills);
            return false;
        }
    }
}