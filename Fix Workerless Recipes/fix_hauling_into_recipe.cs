using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using UnityEngine;
//using UnityEngine.MonoBehaviour;
//using UnityEngine.CoreModule;
using Timberborn.Common;
using Timberborn.BuildingsBlocking;
using Timberborn.InventorySystem;
using Timberborn.Goods;
using System.Linq;
using System.Collections.Generic;
using System;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.Hauling;
using Timberborn.Emptying;



namespace WorkerlessRecipe_HaulingFix
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Fixing hauling priority dropping off way too early", "1.0.6")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                PrioritizeWorkerless = AddKey("PrioritizeWorkerless", true, "Maximize hauling priority on (potentially) workerless buildings"), // bool default value true
            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public bool PrioritizeWorkerless { get; set; }
    }

    namespace Patches
    {
        [HarmonyPatch]
        internal static class HaulingPatch
        {
            [HarmonyPatch(typeof(InventoryFillCalculator), "GetInputFillPercentage", new Type[] {
        typeof(Inventory)
    })]
            private static bool InventoryFillCalculator_GetInputFillPercentage_Patch(Inventory inventory, ref float __result)
            {
                __result = CustomLogic(inventory, inventory.InputGoods, false, true);
                return false;
            }

            [HarmonyPatch(typeof(InventoryFillCalculator), "GetInventoryFillPercentage", new Type[] {
        typeof(Inventory),
        typeof(ReadOnlyHashSet<string>),
        typeof(bool)
    })]
            private static bool InventoryFillCalculator_GetInventoryFillPercentage_Patch(
                Inventory inventory,
                ReadOnlyHashSet<string> goods,
                bool onlyInStock,
                ref float __result)
            {
                __result = CustomLogic(inventory, goods, onlyInStock, false);
                return false;
            }

            private static float CustomLogic(Inventory inventory, ReadOnlyHashSet<string> goods, bool onlyInStock, bool onlyInputPatch)
            {
                float result = 1f;
                if (onlyInputPatch) { result = 0f; }
                List<float> fills = new List<float>
                {
                    result
                };
                foreach (StorableGoodAmount storableGoodAmount in inventory.AllowedGoods)
                {
                    string goodId = storableGoodAmount.StorableGood.GoodId;
                    if (goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0)
                    {
                        int amount = inventory.AmountInStock(goodId);
                        if (!onlyInStock || amount > 0)
                        {
                            fills.Add(Mathf.Clamp01(amount / (float)storableGoodAmount.Amount));                        }
                    }
                }
                if (onlyInputPatch)
                {
                    // different logic for GetInputFillPercentage
                    result = fills.Min<float>();
                }
                else
                {
                    result = fills.Max<float>();
                }
                return result;
            }
        }

        [HarmonyPatch]
        internal static class WorkerlessPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ManufactoryHaulBehaviorProvider), "GetWeightedBehaviors", new Type[] {
        typeof(IList<WeightedBehavior>)
    })]
            private static bool ManufactoryHaulBehaviorProvider_GetWeightedBehaviors_Patch(ref Manufactory ____manufactory, ref IList<WeightedBehavior> weightedBehaviors, BlockableBuilding ____blockableBuilding,
                InventoryFillCalculator ____inventoryFillCalculator, Inventories ____inventories, FillInputWorkplaceBehavior ____fillInputWorkplaceBehavior, EmptyOutputWorkplaceBehavior ____emptyOutputWorkplaceBehavior)
            {
                if (!EP.Config.PrioritizeWorkerless)
                    return true;
                //IL_0079: Unknown result type (might be due to invalid IL or missing references)
                //IL_00ab: Unknown result type (might be due to invalid IL or missing references)
                ProductionIncreaser p;
                bool v = ____manufactory.TryGetComponentFast<ProductionIncreaser>(out p);
                if (!v || !____manufactory || !____manufactory.HasCurrentRecipe || !____blockableBuilding.IsUnblocked)
                    {
                        return true;
                    }

                    foreach (Inventory enabledInventory in ____inventories.EnabledInventories)
                    {
                        if (enabledInventory.IsInput)
                        {
                            float num = Mathf.Clamp01(10*(1f - ____inventoryFillCalculator.GetInputFillPercentage(enabledInventory)));
                            if (num > 0f)
                            {
                                weightedBehaviors.Add(new WeightedBehavior(num, (WorkplaceBehavior)(object)____fillInputWorkplaceBehavior));
                            }
                        }

                        if (enabledInventory.IsOutput)
                        {
                            float outputFillPercentage = Mathf.Clamp01(2 * ____inventoryFillCalculator.GetOutputFillPercentage(enabledInventory));
                            if (outputFillPercentage > 0f)
                            {
                                weightedBehaviors.Add(new WeightedBehavior(outputFillPercentage, (WorkplaceBehavior)(object)____emptyOutputWorkplaceBehavior));
                            }
                        }
                    }
                return false;
            }
        }
        }
}