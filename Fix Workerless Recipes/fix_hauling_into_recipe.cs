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
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Improve /Prioritize by Haulers/", "1.0.9.3")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                Prioritize_threshold = AddKey("/Prioritize by Haulers/ Settings", "threshold", 0.25f, "Base priority threshold for appying prioritization, ignored for No-Worker-needed Workshops if those tweaks are enabled"), // float default value 0.25
                Prioritize_strength = AddKey("/Prioritize by Haulers/ Settings", "strength", 1f, "Prioritization strength to be applied with ingame toggle"), // float default value 1
                Workerless_toggle = AddKey("Haul to No-Worker Workshop Settings", "enable", true, "Give special treatment to No-Worker-needed Workshops?"), // bool default value true
                Workerless_priority = AddKey("Haul to No-Worker Workshop Settings", "priority", 1.11f, "Extra Prioritization strength for workerless manufacturies \n\t (tied to /Prioritize by Haulers/ toggle)"), // float default value 1.11
                Workerless_floor = AddKey("Haul to No-Worker Workshop Settings", "floor", 0.89f, "Basic (calculated by the HaulCandidate) prioritization weight is always at least ..."), // float default value 0.89

            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public float Prioritize_threshold { get; set; }
        public float Prioritize_strength { get; set; }
        public float Workerless_priority { get; set; }
        public float Workerless_floor { get; set; }
        public bool Workerless_toggle { get; set;}

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
                bool _workerless = EP.Config.Workerless_toggle && ____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _);
                bool _prioritize = ____haulPrioritizable.Prioritized && (weight >= EP.Config.Prioritize_threshold || _workerless);
                Log.Debug("initial weight:" + weight);
                __result = _workerless ? Mathf.Clamp(weight, Mathf.Max(EP.Config.Prioritize_threshold, EP.Config.Workerless_floor), 5f) : weight;
                Log.Debug("checked if HaulCandidate is a No-Workers Manufactory("+_workerless+") ... corrected initial value:" + __result);

                __result += _prioritize ? (_workerless ? EP.Config.Prioritize_strength + EP.Config.Workerless_priority : EP.Config.Prioritize_strength) : 0f;
                Log.Debug("checked if HaulCandidate is meant to be Prioritized (" + _prioritize + ") ... tmp value" + __result);

                __result = Mathf.Clamp(__result, 0f, 10f);
                Log.Debug($"final haul priority for {____haulPrioritizable.GameObjectFast.name}(within 0-10f): {__result}");
                return false;
            }
        }
    }
}