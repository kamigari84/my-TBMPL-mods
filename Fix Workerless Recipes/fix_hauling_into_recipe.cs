using HarmonyLib;
using System;
using Timberborn.Workshops;
using Timberborn.Hauling;
using Timberborn.WorkSystem;
using Timberborn.Attractions;
using Timberborn.GoodConsumingBuildingSystem;
using BepInEx;
using BepInEx.Configuration;
using TimberApi.ModSystem;
using TimberApi.ConsoleSystem;
using System.IO;
using File = System.IO.File;


namespace WorkerlessRecipe_HaulingFix
{
    [BepInPlugin(EP.mod_guid, EP.mod_desc, EP.mod_version)]
    public class EP : BaseUnityPlugin, IModEntrypoint
    {
        private const string mod_version = "1.0.9.8";
        private const string mod_guid = "Hauling2RecipeFix";
        private const string mod_desc = "Improve /Prioritize by Haulers/";
        private static ConfigEntry<float> _Prioritize_threshold;
        private static ConfigEntry<float> _Prioritize_strength;
        private static ConfigEntry<float> _Workerless_floor;
        private static ConfigEntry<bool> _Workerless_toggle;
        private static ConfigEntry<bool> _Workplace_deprioritize;
        private static Harmony harmony = new Harmony(EP.mod_guid);
        public static float Prioritize_threshold { get => _Prioritize_threshold.Value; private set => _Prioritize_threshold.Value = value; }
        public static float Prioritize_strength { get => _Prioritize_strength.Value; private set => _Prioritize_strength.Value = value; }
        public static float Workerless_floor { get => _Workerless_floor.Value; private set => _Workerless_floor.Value = value; }
        public static bool Workerless_toggle { get => _Workerless_toggle.Value; private set => _Workerless_toggle.Value = value; }
        public static bool Workplace_deprioritize { get => _Workplace_deprioritize.Value; private set => _Workplace_deprioritize.Value = value; }

        public void Awake()
        {
            _Prioritize_threshold = Config.Bind("/Prioritize by Haulers/ Settings",      // The section under which the option is shown
                                         "threshold",  // The key of the configuration option in the configuration file
                                         0.005f, // The default value
                                         new ConfigDescription($"Threshold for applying /Prioritize by Haulers/\n Current value: {_Prioritize_threshold.Value}", new AcceptableValueRange<float>(0f, 1f))
                                         ); // Description of the option to show in the config file
            _Prioritize_strength = Config.Bind("/Prioritize by Haulers/ Settings",      // The section under which the option is shown
                                         "strength",  // The key of the configuration option in the configuration file
                                         1.1f, // The default value
                                         "Strength of /Prioritize by Haulers/"); // Description of the option to show in the config file
            _Workerless_toggle = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "enable",
                                             true,
                                             "Give special treatment to Workerless Manufactories ? ");
            _Workerless_floor = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
                                             "floor",
                                             1.2f,
                                             "For NoWorker buildings, alway increase 'priority' by ... ");
            _Workplace_deprioritize = Config.Bind("NoWorker good-consumer/producer Hauling Settings",
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
    namespace Patches
    {
        [HarmonyPatch]
        internal static class HaulingPatch
        {

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HaulCandidate), "PrioritizeAndValidate", new Type[] {
               typeof(float)
    })]
            private static bool HaulCandidate_PrioritizeAndValidate_Patch(
                float weight,
                HaulPrioritizable ____haulPrioritizable,
                ref float __result)
            {
                __result = weight;
                bool Appliable = ____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _) || (____haulPrioritizable.GameObjectFast.TryGetComponent<GoodConsumingBuilding>(out _) && ____haulPrioritizable.GameObjectFast.TryGetComponent<Workplace>(out _)) || ____haulPrioritizable.GameObjectFast.TryGetComponent<GoodConsumingAttraction>(out _);
                if (__result != 0)
                {
                    if (EP.Workerless_toggle && Appliable)
                    {
                        __result += EP.Workerless_floor;
                    }

                    if ((____haulPrioritizable.Prioritized && (__result >= EP.Prioritize_threshold)))
                    {
                        __result += EP.Prioritize_strength;
                    }

                }

                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ProductionIncreaser), "OnEnterFinishedState")]
            private static void DePrioritizer(Manufactory ____manufactory)
            {
                if (EP.Workplace_deprioritize && ____manufactory.GameObjectFast.TryGetComponent<WorkplacePriority>(out var priority))
                {
                    priority.SetPriority(Timberborn.PrioritySystem.Priority.VeryLow);
                }
            }

        }
    }

}