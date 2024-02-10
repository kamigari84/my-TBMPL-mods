using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using System;
using Timberborn.Hauling;
using TBMPLCore.Plugin.Logs;



namespace PrioritizeByHaulers_Config
{
    [TBMPLVersionCheck("https://github.com/kamigari84/my-TBMPL-mods/raw/update5/Fix%20Workerless%20Recipes/version.json")]
    [TBMPL(TBMPL.Prefix + "PrioritizeConfig", "Improve /Prioritize by Haulers/", "1.1.0")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                Prioritize_threshold = AddKey("/Prioritize by Haulers/ Settings", "threshold", 0.005f, "Base priority threshold for appying prioritization"), // float default value 0.005
                Prioritize_strength = AddKey("/Prioritize by Haulers/ Settings", "strength", 1.1f, "Prioritization strength to be applied with ingame toggle"), // float default value 1.1               
            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public float Prioritize_threshold { get; set; }
        public float Prioritize_strength { get; set; }
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
                if (____haulPrioritizable.Prioritized && (__result >= EP.Config.Prioritize_threshold))
                {
                    __result += EP.Config.Prioritize_strength;
                }
                return false;
            }
        }
    }
}