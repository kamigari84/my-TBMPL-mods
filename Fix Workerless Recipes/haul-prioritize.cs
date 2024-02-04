using HarmonyLib;
using System;
using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using TBMPLCore.Plugin.Logs;
using Timberborn.Hauling;
using Timberborn.Workshops;



namespace WorkerlessRecipe_HaulingFix
{
    //    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "HaulingImproved", "Improve /Prioritize by Haulers/ LITE", "0.0.2")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

    }
    internal sealed class EPConfig : BaseConfig { }


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
                Log.Debug("initial weight:" + __result);
                if (____haulPrioritizable.GameObjectFast.TryGetComponent<ProductionIncreaser>(out _))
                {
                    Log.Debug("is Workerless");
                    __result += 2f;
                }
                if (____haulPrioritizable.Prioritized)
                {
                    Log.Debug("is Prioritized");
                    __result += 1f;
                }
                //__result = Mathf.Clamp(__result, 0f, 10f);
                Log.Debug($"final haul priority for {____haulPrioritizable.GameObjectFast.name}: {__result}");
                return false;
            }
        }
    }
}
