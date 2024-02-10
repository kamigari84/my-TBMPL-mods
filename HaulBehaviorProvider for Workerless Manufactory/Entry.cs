using HarmonyLib;
using HaulBehaviorProvider_for_Workerless_Manufactory.HaulBehaviourProvider.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TimberApi.ConfigSystem;
using TimberApi.ConsoleSystem;
using TimberApi.ModSystem;
using Timberborn.BuildingsBlocking;
using Timberborn.Common;
using Timberborn.Emptying;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Hauling;
using Timberborn.InventorySystem;
using Timberborn.TemplateSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine.UIElements.Collections;


namespace HaulBehaviorProvider_for_Workerless_Manufactory
{
    public class ModEntry : IModEntrypoint
    {
        public static NoWorkerHaulConfig Config;

        public void Entry(IMod mod, IConsoleWriter consoleWriter)
        {
            Config = mod.Configs.Get<NoWorkerHaulConfig>();
            Harmony harmony = new Harmony("Kamigari84.NoWorkerHaul");
            harmony.PatchAll();
        }
    }

    public class NoWorkerHaulConfig : IConfig
    {
        public string ConfigFileName => "config";
        public float NoWorkerBonus { get; set; }
        public float EmptyingThreshold { get; set; }

        public float FillingThreshold { get; set; }

        public bool RemoveUnneededWorkplaces { get; set; }
        public NoWorkerHaulConfig()
        {
            NoWorkerBonus = 0.8f;
            EmptyingThreshold = 0.34f;
            FillingThreshold = 0f;
            RemoveUnneededWorkplaces = false;
        }
    }

    [HarmonyPatch]
    static class SimplePatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkshopsTemplateModuleProvider), nameof(WorkshopsTemplateModuleProvider.Get))]
        private static bool Replace_ManufactoryHaulBehaviorProvider_InTemplate(ManufactoryInventoryInitializer ____manufactoryInventoryInitializer, ref TemplateModule __result, WorkshopsTemplateModuleProvider __instance)
        {
            //DebugEx.Info("Trying to patch {0} 's {1}", __instance.GetType().FullName, "TemplateModule Get");
            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDedicatedDecorator(____manufactoryInventoryInitializer);
            builder.AddDecorator<Manufactory, AutoEmptiable>();
            builder.AddDecorator<Manufactory, Emptiable>();
            builder.AddDecorator<Manufactory, HaulCandidate>();
            builder.AddDecorator<Manufactory, ManufactoryHaul>();//replaced component
            builder.AddDecorator<Manufactory, ManufactoryInputChecker>();
            builder.AddDecorator<ManufactoryInputChecker, LackOfResourcesStatus>();
            builder.AddDecorator<Manufactory, NoRecipeStatus>();
            builder.AddDecorator<Workshop, WorkshopProductivityCounter>();
            builder.AddDecorator<Workshop, WorkplacePowerConsumptionSwitch>();
            builder.AddDecorator<Worker, WorkplaceWorkStarter>();
            MethodInfo methodInfo = typeof(WorkshopsTemplateModuleProvider).GetMethod("InitializeBehaviors", BindingFlags.NonPublic | BindingFlags.Static);
            var parameters = new object[] { builder };
            methodInfo.Invoke(null, parameters);
            __result = builder.Build();
            return false;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionIncreaser), "OnEnterFinishedState")]
        private static void DePrioritizer(Manufactory ____manufactory)
        {
            if (ModEntry.Config.RemoveUnneededWorkplaces && ____manufactory.GameObjectFast.TryGetComponent<WorkplacePriority>(out var priority))
            {
                priority.SetPriority(Timberborn.PrioritySystem.Priority.VeryLow);
            }
        }
    }

    [HarmonyPatch]
    static class ComplexPatcher
    {
        [HarmonyTargetMethod]
        static MethodBase CalculateMethod() => AccessTools.Method(typeof(GoodConsumingBuildingSystemConfigurator).GetNestedType("TemplateModuleProvider"), "Get");

        private static bool Prefix(ManufactoryInventoryInitializer ____goodConsumingBuildingInventoryInitializer, ref TemplateModule __result, WorkshopsTemplateModuleProvider __instance)
        {
            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDecorator<GoodConsumingBuilding, AutoEmptiable>();
            builder.AddDecorator<GoodConsumingBuilding, Emptiable>();
            builder.AddDecorator<GoodConsumingBuilding, FillInput>();//replaced component
            builder.AddDecorator<GoodConsumingBuilding, GoodConsumingBuildingStatusInitializer>();
            builder.AddDecorator<GoodConsumingBuildingStatusInitializer, LackOfResourcesStatus>();
            builder.AddDecorator<GoodConsumingBuildingStatusInitializer, NoHaulingPostStatus>();
            builder.AddDedicatedDecorator(____goodConsumingBuildingInventoryInitializer);
            builder.AddDecorator<GoodConsumingBuilding, EmptyInventoriesWorkplaceBehavior>();
            builder.AddDecorator<GoodConsumingBuilding, FillInputWorkplaceBehavior>();
            builder.AddDecorator<GoodConsumingBuilding, RemoveUnwantedStockWorkplaceBehavior>();
            __result = builder.Build();
            return false;
        }
    }

}
