using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using UnityEngine;
using System;
using Timberborn.Workshops;
using Timberborn.Hauling;
using TBMPLCore.Plugin.Logs;



namespace WorkerlessRecipe_HaulingFix
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Improve /Prioritize by Haulers/", "1.0.9.2")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                threshold = AddKey("Settings", "threshold", 0.25f, "Base priority threshold for appying prioritization,\n\t workerless manufactories are always at, at least, this level of importance"), // float default value 0.25
                strength = AddKey("Settings", "strength", 0.75f, "Prioritization strength to be applied with ingame toggle"), // int default value 0.75
                workereless = AddKey("Settings", "workereless", 0.75f, "Extra Prioritization strength for workerless manufacturies \n\t set to 0 to disable - otherwise it always applies (ignore toggle)"), // int default value 0.75

            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public float threshold { get; set; }
        public float strength { get; set; }
        public float workereless { get; set; }

    }


    namespace Patches
    {
        [HarmonyPatch]
        internal static class HaulingPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HaulCandidate), "PrioritizeAndValidate", new Type[] {
               typeof(float)
    })]
            private static void HaulCandidate_PrioritizeAndValidate_Patch(float weight,
                                                                          HaulPrioritizable ____haulPrioritizable,
                                                                          ref float __result)
            {
                bool _workerless = ____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _);
                bool _prioritize = ____haulPrioritizable.Prioritized && (__result >= EP.Config.threshold || _workerless);
                Log.Debug("initial weight:" + weight);
                __result = _workerless ? Mathf.Clamp(weight, EP.Config.threshold, 1f) : weight;
                Log.Debug("checked if HaulCandidate is a No-Workers Manufactory ... corrected initial value:" + __result);

                __result += (EP.Config.workereless > 0 && _workerless) ? EP.Config.workereless : 0f;
                Log.Debug("checked if HaulCandidate is a No-Workers Manufactory ... tmp value:" + __result);

                __result += _prioritize ? EP.Config.strength : 0f;
                Log.Debug("checked if HaulCandidate is meant to be Prioritized ... tmp value" + __result);

                __result = Mathf.Clamp(__result, 0f, 5f);
                Log.Debug($"final haul priority for {____haulPrioritizable.GameObjectFast.name}(within 0-5f): {__result}");
            }
        }
    }
}