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
    [TBMPL(TBMPL.Prefix + "Hauling22RecipeFix", "Fixing hauling priority to unmanned production buildings dropping off way too early", "1.0.0")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig();
        }

    }
    internal sealed class EPConfig : BaseConfig{}


    // Some of our patches for the game
    [HarmonyPatch]
    internal static class Patches
    {
    private static MethodInfo TargetMethod()
    {
	return AccessTools.Method(typeof(InventoryFillCalculator), "GetInventoryFillPercentage");
    }
//HarmonyPrefix patch this method in Timberborn.InventorySystem.InventoryFillCalculator
	static void Prefix (Inventory inventory, ReadOnlyHashSet<string> goods, bool onlyInStock, ref float __result)
	//private float GetInventoryFillPercentage(Inventory inventory, ReadOnlyHashSet<string> goods, bool onlyInStock)
	{
		float fill = 1.0f; // keep track of the average filling of all ingredients
		foreach (StorableGoodAmount allowedGood in inventory.AllowedGoods)
		{
			string goodId = allowedGood.StorableGood.GoodId;
			if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
			{
				int num3 = inventory.AmountInStock(goodId);
				if (!onlyInStock || num3 > 0)
				{
					// num += allowedGood.Amount;
					// num2 += num3;
					fill *= Mathf.Clamp01((float)num3 / (float)allowedGood.Amount); // update how filled up we are (if any good is at 0 then we are not filled at all)
				}
			}
		}
		__result = fill;
	}
    }
}
