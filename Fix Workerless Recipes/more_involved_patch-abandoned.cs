using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using UnityEngine;
using Timberborn.Common;
using Timberborn.BuildingsBlocking;
using Timberborn.InventorySystem;
using System.Collections.Generic;
using System;
using Timberborn.Workshops;
using Timberborn.Hauling;
using Timberborn.Emptying;
using TBMPLCore.Plugin.Logs;
using System.Linq;
using Timberborn.Goods;



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
                prioritizationPatch = AddKey("prioritizationPatch", true, "[MAIN] Patch to let /Prioritize this building by haulers/ boost hauling priority more and longer"), // bool default value true
                manufactoryPatch = AddKey("manufactoryPatch", false, "Patch to let fill/empty hauling priority on production buildings be the higest reasonable possible" +
                "\nIt could cause some performance issues"), // bool default value false
                PrioritizeWorkerless = AddKey("PrioritizeWorkerless", false, "Raise overall hauling priority for the sake workerless manufactory buildings" +
                "\n Mostly relevant for mods like WaterBeaverOverhaul with a good few unmanned production buildings." +
                "\n Can only work alongside prioritizationPatch (greater increase in priority - for all) and/or manufactoryPatch (raised priority for workerless manufactory buildings)"), // bool default value false
                inventoryPatch = AddKey("inventoryPatch", false, "This is an obsolete relic" +
                "\nChange inventory fullness to be calculated as the average fullness of its parts?" +
                "\nThis is likely to cause ingame issues & corrupt your save"), // bool default value false

            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public bool PrioritizeWorkerless { get; set; }
        public bool inventoryPatch { get; set; }
        public bool prioritizationPatch { get; set; }
        public bool manufactoryPatch { get; set; }

    }


    namespace Patches
    {
        [HarmonyPatch]
        internal static class HaulingPatch
        {
            public enum InventoryFullnessMode {
                Input,
                Output,
                Natural,
                Default,
                OnlyInStock
                }
            private static float CustomLogic(Inventory inventory, ReadOnlyHashSet<string> goods, InventoryFullnessMode modes = InventoryFullnessMode.Natural)
            {
                int count = 0;
                float result;

                if (modes.HasFlag(InventoryFullnessMode.Input))
                {
					Log.Debug("mode: input");
                    {
                        result = 1f;
                        foreach (var amount in from StorableGoodAmount storableGoodAmount in inventory.AllowedGoods
                                               let goodId = storableGoodAmount.StorableGood.GoodId
                                               where goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0
                                               let amount = (float)inventory.AmountInStock(goodId) / (float)storableGoodAmount.Amount
                                               select amount)
                        {
                            Log.Debug("Tmp ratio: " + amount);
                            if (!modes.HasFlag(InventoryFullnessMode.OnlyInStock) || amount > 0)
                            {
                                result = Mathf.Min(result, amount);
                            }

                            Log.Debug("New min ratio: " + result);
                        }
                    }
                }
                else if (modes.HasFlag(InventoryFullnessMode.Output))
                {
                    Log.Debug("mode: output");
                    result = 0f;
                    foreach (var amount in from StorableGoodAmount storableGoodAmount in inventory.AllowedGoods
                                           let goodId = storableGoodAmount.StorableGood.GoodId
                                           where goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0
                                           let amount = (float)inventory.AmountInStock(goodId) / (float)storableGoodAmount.Amount
                                           select amount)
                    {
                        Log.Debug("Tmp ratio: " + amount);
                        if (!modes.HasFlag(InventoryFullnessMode.OnlyInStock) || amount > 0)
                        {
                            result = Mathf.Max(result, amount);
                        }

                        Log.Debug("New min ratio: " + result);
                    }
                }
                else if (modes.HasFlag(InventoryFullnessMode.Natural))
                {
					Log.Debug("mode: natural");
                    result = 0f;
                    foreach (var (storableGoodAmount, amount) in from StorableGoodAmount storableGoodAmount in inventory.AllowedGoods
                                                                 let goodId = storableGoodAmount.StorableGood.GoodId
                                                                 where goods.Contains(goodId) && inventory.LimitedAmount(goodId) > 0
                                                                 let amount = (float)inventory.AmountInStock(goodId) / (float)storableGoodAmount.Amount
                                                                 where !modes.HasFlag(InventoryFullnessMode.OnlyInStock) || amount > 0
                                                                 select (storableGoodAmount, amount))
                    {
                        result += (Mathf.Clamp01(amount / (float)storableGoodAmount.Amount));
                        count++;
                    }

                    result = Mathf.Clamp01(result / count);
					Log.Debug("calculated ratio: "+result);
                } else
                {
					Log.Debug("mode: default");
                    result = (float)inventory.TotalAmountInStock / (float)inventory.Capacity;
					Log.Debug("ratio: "+result);
                }
                
                return Mathf.Clamp01(result);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ManufactoryHaulBehaviorProvider), "GetWeightedBehaviors", new Type[] {
        typeof(IList<WeightedBehavior>)
    })]
            private static bool ManufactoryHaulBehaviorProvider_GetWeightedBehaviors_Patch(ref Manufactory ____manufactory, ref IList<WeightedBehavior> weightedBehaviors, BlockableBuilding ____blockableBuilding,
                Inventories ____inventories, FillInputWorkplaceBehavior ____fillInputWorkplaceBehavior, EmptyOutputWorkplaceBehavior ____emptyOutputWorkplaceBehavior)
            {
                if (EP.Config.manufactoryPatch)
                {
                    ProductionIncreaser p;
                    if (____manufactory && ____manufactory.HasCurrentRecipe && ____blockableBuilding.IsUnblocked)
                    {
                        Log.Debug("Operating Manufactory: " + ____manufactory.name);
                        bool v = EP.Config.PrioritizeWorkerless && ____manufactory.TryGetComponentFast<ProductionIncreaser>(out p);
                        foreach (Inventory enabledInventory in ____inventories.EnabledInventories)
                        {

                            if (enabledInventory.IsInput)
                            {
                                Log.Debug(____manufactory.name + " - input - ");
                                float num = (1f - CustomLogic(enabledInventory, enabledInventory.InputGoods, InventoryFullnessMode.Input));
                                Log.Debug(____manufactory.name + " input ratio : " + num);
                                if (num > 0.05f)
                                {
                                    //num += 0.5f;
                                    if (v) { num += 0.8f; }
                                    Log.Debug(____manufactory.name + " fill input with weight " + num);
                                    weightedBehaviors.Add(new WeightedBehavior(Mathf.Clamp01(num), ____fillInputWorkplaceBehavior));
                                }
                            }
                            if (enabledInventory.IsOutput)
                            {
                                Log.Debug(____manufactory.name + " - output - ");
                                float num = CustomLogic(enabledInventory, enabledInventory.OutputGoods, InventoryFullnessMode.Output);
                                Log.Debug(____manufactory.name + " output ratio : "+num);

                                if (num > 0.05f)
                                {
                                    //num += 0.5f;
                                    if (v) { num += 0.6f; }
                                    Log.Debug(____manufactory.name + " empty output with weight " + num);
                                    weightedBehaviors.Add(new WeightedBehavior(Mathf.Clamp01(num), ____emptyOutputWorkplaceBehavior));
                                }
                            }
                        }
                    }
                }
                return !EP.Config.manufactoryPatch;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(FillInputHaulBehaviorProvider), "GetWeightedBehaviors", new Type[] {
        typeof(IList<WeightedBehavior>)
    })]
            private static bool FillInputHaulBehaviorProvider_GetWeightedBehaviors_Patch(ref IList<WeightedBehavior> weightedBehaviors, BlockableBuilding ____blockableBuilding,
     Inventories ____inventories, FillInputWorkplaceBehavior ____fillInputWorkplaceBehavior, GameObject __instance)
            {
                if (EP.Config.manufactoryPatch)
                {
                    ProductionIncreaser p;
                    if (____blockableBuilding.IsUnblocked)
                    {
                        bool v = EP.Config.PrioritizeWorkerless && __instance.TryGetComponent<ProductionIncreaser>(out p);
                        foreach (Inventory enabledInventory in ____inventories.EnabledInventories)
                        {
                            float num = 1f - CustomLogic(enabledInventory, enabledInventory.InputGoods, InventoryFullnessMode.Input);
                            if (num > 0f)
                            {
                                //num += 0.5f;
                                if (v) { num += 0.8f; }
                                Log.Debug("fill input with weight " + num);
                                weightedBehaviors.Add(new WeightedBehavior(Mathf.Clamp01(num), ____fillInputWorkplaceBehavior));
                            }
                        }
                    }
                }
                return !EP.Config.manufactoryPatch;
            }

            [HarmonyPrefix]
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
                if (EP.Config.inventoryPatch)
                {
                    InventoryFullnessMode m = InventoryFullnessMode.Natural;
                    if (onlyInStock) { m |= InventoryFullnessMode.OnlyInStock; }
                    __result = CustomLogic(inventory, goods, m);
                }
                return !EP.Config.inventoryPatch;
            }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HaulCandidate), "PrioritizeAndValidate", new Type[] {
               typeof(float)
    })]
        private static bool HaulCandidate_PrioritizeAndValidate_Patch(float weight, HaulPrioritizable ____haulPrioritizable, ref float __result)
            {
                if (EP.Config.prioritizationPatch)
                {
                    Log.Debug("initial weight:" + weight);
                    if (weight < 0f || weight > 1f)
                    {
                        Log.Debug("weight should be between 0 and 1!");
                        weight = Mathf.Clamp(weight, 0f, 1f);
                    }
                    /*if (weight != 0f)
                    {
                        weight = Mathf.Clamp(weight, 0.6f, 1f);
                    }*/
                    __result = weight;
                    if (____haulPrioritizable.Prioritized && (double)weight >= 0.05)
                    {
                        __result += 0.8f;
                    }
                    if (!EP.Config.manufactoryPatch && ____haulPrioritizable.Prioritized && EP.Config.PrioritizeWorkerless && (double)weight >= 0.05)
                    {
                        __result += 0.6f;
                    }
                    Log.Debug("validated weight: " + __result);
                }
                return !EP.Config.prioritizationPatch;
            }
        }
    }
}