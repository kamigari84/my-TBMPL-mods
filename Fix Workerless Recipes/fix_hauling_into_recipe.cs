using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using System;
using Timberborn.Workshops;
using Timberborn.Hauling;
using Timberborn.WorkSystem;


namespace WorkerlessRecipe_HaulingFix
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Improve /Prioritize by Haulers/", "1.0.9.4")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                Prioritize_threshold = AddKey("/Prioritize by Haulers/ Settings", "threshold", 0.25f, "Base priority threshold for appying prioritization"), // float default value 0.25
                Prioritize_strength = AddKey("/Prioritize by Haulers/ Settings", "strength", 1.1f, "Prioritization strength to be applied with ingame toggle"), // float default value 1.1
                Workerless_toggle = AddKey("Workerless Manufactory Hauling Settings", "enable", true, "Give special treatment to Workerless Manufactories?"), // bool default value true
                Workerless_floor = AddKey("Workerless Manufactory Hauling Settings", "floor", 1.2f, "For Workerless Manufactories, always increase prioritization weight by \n\tshould be >= threshold"), // float default value 1.2
                Workplace_deprioritize = AddKey("Workerless Manufactory Hauling Settings", "lower", true, "Set Workplace Priority of potentianlly-workerless Manufactories to lowest?"), // bool default value true

            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public float Prioritize_threshold { get; set; }
        public float Prioritize_strength { get; set; }
        public float Workerless_floor { get; set; }
        public bool Workerless_toggle { get; set; }
        public bool Workplace_deprioritize { get; set; }
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
                if (__result == 0)
                {
                    return false;
                }

                if (EP.Config.Workerless_toggle && ____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _))
                {
                    __result += EP.Config.Workerless_floor;
                }


                if ((____haulPrioritizable.Prioritized && (__result >= EP.Config.Prioritize_threshold)))
                {
                    __result += EP.Config.Prioritize_strength;
                }

                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ProductionIncreaser), "OnEnterFinishedState")]
            private static void DePrioritizer(Manufactory ____manufactory)
            {
                if (EP.Config.Workplace_deprioritize && ____manufactory.GameObjectFast.TryGetComponent<WorkplacePriority>(out var priority))
                {
                    priority.SetPriority(Timberborn.PrioritySystem.Priority.VeryLow);
                }
            }

        }
    }

}