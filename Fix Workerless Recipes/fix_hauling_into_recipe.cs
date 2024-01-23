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
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Improve /Prioritize by Haulers/,\n especially for workerless manufacturers", "1.0.9.1")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                threshold = AddKey("Settings", "threshold", 0.05f, "Base priority threshold for appying prioritization"), // float default value 0.05
                strength = AddKey("Settings", "strength", 0.75f, "Prioritization strength"), // int default value 0.75
                workereless = AddKey("Settings", "workereless", 0.75f, "Extra Prioritization strength for workerless manufcaturies \n\t set to 0 to disable"), // int default value 0.75

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
            private static void HaulCandidate_PrioritizeAndValidate_Patch(float weight, HaulPrioritizable ____haulPrioritizable, ref float __result)
            {
                Log.Debug("initial weight:" + weight);
                __result = (____haulPrioritizable.GameObjectFast.TryGetComponent<Manufactory>(out _) && weight > 0f) ? Mathf.Clamp(weight, EP.Config.threshold, 1f) : weight;
                Log.Debug("checked if HaulCandidate is a Manufactory ... tmp result" + __result);
                __result += (____haulPrioritizable.Prioritized && __result >= EP.Config.threshold) ? EP.Config.strength : 0f;
                Log.Debug("checked if HaulCandidate is meant to be Prioritized ... tmp result" + __result);
                __result += (EP.Config.workereless > 0f && ____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _)) ? EP.Config.workereless : 0f;
                Log.Debug("checked if HaulCandidate is UnManned (has NeedIncreaser) ... tmp result" + __result);
                weight = Mathf.Clamp(weight, 0f, 2f);
                Log.Debug("updated weight: " + weight);
            }
        }
    }
}