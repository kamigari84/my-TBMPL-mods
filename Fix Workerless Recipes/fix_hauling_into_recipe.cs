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
    [TBMPL(TBMPL.Prefix + "Hauling2RecipeFix", "Improve /Prioritize by Haulers/,\n especially for workerless manufacturers", "1.0.9")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig { };
        }

    }
    internal sealed class EPConfig : BaseConfig {}


    namespace Patches
    {
        [HarmonyPatch]
        internal static class HaulingPatch
        {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HaulCandidate), "PrioritizeAndValidate", new Type[] {
               typeof(float)
    })]
        private static bool HaulCandidate_PrioritizeAndValidate_Patch(float weight, HaulPrioritizable ____haulPrioritizable, ref float __result)
            {
                float w;
                    Log.Debug("initial weight:" + weight);
                    if (weight < 0f || weight > 1f)
                    {
                        Log.Debug("weight should be between 0 and 1!");
                        //weight = Mathf.Clamp01(weight);
                    }
                    w = weight;
                    if (____haulPrioritizable.Prioritized && (double)weight >= 0.05f)
                    {
                        w += 0.75f;
                    }
                    if (____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out var increaser) && (double)weight >= 0.05f)
                    {
                        w += 0.75f;
                    }
                    w = Mathf.Clamp(w, 0f, 2f);
                    Log.Debug("validated weight: " + w);
                    __result = w;
                    return false;
            }
        }
    }
}