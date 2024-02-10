using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NoWorkerHaul.HaulBehaviourProvider.Core;
using System.IO;
using System.Reflection;
using TimberApi.ConsoleSystem;
using TimberApi.ModSystem;
using Timberborn.Emptying;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Hauling;
using Timberborn.TemplateSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;


namespace NoWorkerHaul
{

    [BepInPlugin(EP.mod_guid, EP.mod_desc, EP.mod_version)]
    public class EP : BaseUnityPlugin, IModEntrypoint
    {
        private const string mod_version = "0.9.0";
        private const string mod_guid = "NoWorkerHaul";
        private const string mod_desc = "Extra hauling priority for NoWorker good-consuming/producing buildings";
        private static ConfigEntry<float> _Workerless_floor;
        private static ConfigEntry<float> _FillingThreshold;
        private static ConfigEntry<float> _EmptyingThreshold;

        private static ConfigEntry<bool> _Workerless_toggle;
        private static ConfigEntry<bool> _RemoveUnneededWorkplaces;
        private static Harmony harmony = new Harmony(EP.mod_guid);

        public static float Workerless_floor { get => _Workerless_floor.Value; private set => _Workerless_floor.Value = value; }
        public static float FillingThreshold { get => _FillingThreshold.Value; private set => _FillingThreshold.Value = value; }

        public static float EmptyingThreshold { get => _EmptyingThreshold.Value; private set => _EmptyingThreshold.Value = value; }

        public static bool Workerless_toggle { get => _Workerless_toggle.Value; private set => _Workerless_toggle.Value = value; }
        public static bool RemoveUnneededWorkplaces { get => _RemoveUnneededWorkplaces.Value; private set => _RemoveUnneededWorkplaces.Value = value; }

        public void Awake()
        {
            _Workerless_toggle = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "enable",
                                             true,
                                             "Give special treatment to Workerless Manufactories ? ");
            _Workerless_floor = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "floor",
                                             1.2f,
                                             "For NoWorker buildings, alway increase 'priority' by ... ");
            _FillingThreshold = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "fill_threshold",
                                             0.005f,
                                             new ConfigDescription($"Increase haul priority for filling if less empty than -> {_FillingThreshold.Value} ", new AcceptableValueRange<float>(0f, 1f))
                                             );
            _EmptyingThreshold = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "empty_threshold",
                                             0.34f,
                                             new ConfigDescription($"Increase haul priority for emptying if more full than -> {_EmptyingThreshold.Value} ", new AcceptableValueRange<float>(0f, 1f)));
            _RemoveUnneededWorkplaces = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "lower",
                                             false,
                                             "For potentially NoWorker buildings, reduce workplace priority to lowest ? ");
            var TAPI_mod_declaration = Path.Combine(Paths.PluginPath, "mod.json");
            if (!File.Exists(TAPI_mod_declaration))
            {
                File.WriteAllText(TAPI_mod_declaration,
                                  $"{{\r\n  \"Name\": \"{mod_desc}/\",                     // Name of the mod\r\n" +
                                  $"  \"Version\": \"{mod_version}\",                       // Version of the mod\r\n" +
                                  $"  \"UniqueId\": \"{mod_guid}\",     // Unique identifier of the mod\r\n" +
                                  $"  \"MinimumApiVersion\": \"0.6.5\",             // Minimun TimberAPI version this mod needs\r\n" +
                                  $"  \"MinimumGameVersion\": \"0.5.7\",            // Minimun game version this mod needs (0.2.8 is the lowest that works with TimberAPI v0.5)\r\n" +
                                  $"  \"EntryDll\": \"{Path.Combine(Paths.PluginPath, GetType().Namespace + ".dll")}\", // Optional. The entry dll if the mod has custom code\r\n" +
                                  $"  \"Assets\": [                               // Optional. The Prefix for the asset bundle and the scenes where they should be loaded. \r\n" +
                                  $"    {{\r\n      \"Prefix\": \"{mod_guid}\",\r\n      \"Scenes\": [\r\n        \"All\"\r\n      ]\r\n    }}\r\n  ]\r\n}}");
            }
        }
        public void Entry(IMod mod, IConsoleWriter consoleWriter)
        {
            harmony.PatchAll();
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
            if (EP.RemoveUnneededWorkplaces && ____manufactory.GameObjectFast.TryGetComponent<WorkplacePriority>(out var priority))
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
