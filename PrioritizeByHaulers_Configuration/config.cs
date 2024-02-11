using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using Timberborn.Hauling;
using Helpers;
using TimberApi.ModSystem;
using TimberApi.ConsoleSystem;



namespace PrioritizeByHaulers_Config
{
    [BepInPlugin(EP.mod_guid, EP.mod_desc, EP.mod_version)]
    public class EP : BaseUnityPlugin, IModEntrypoint
    {
        private const string mod_version = "1.1.0";
        private const string mod_guid = "PrioritizeConfig";
        private const string mod_desc = "Configure /Prioritize by Haulers/";
        private static ConfigEntry<float> _Prioritize_threshold;
        private static ConfigEntry<float> _Prioritize_strength;

        private static Harmony harmony;

        public static float Prioritize_threshold { get => _Prioritize_threshold.Value; private set => _Prioritize_threshold.Value = value; }
        public static float Prioritize_strength { get => _Prioritize_strength.Value; private set => _Prioritize_strength.Value = value; }

        public EP()
        {
            EPHelpers.TAPI_declarer(GUID: mod_guid, Version: mod_version, Desc: mod_desc);

            harmony = new Harmony(EP.mod_guid);
            _Prioritize_strength = Config.Bind("Configure /Prioritize by Haulers/",
                                             "strength",
                                             1.2f,
                                             "'Prioritize by haulers' adds x weight ");
            _Prioritize_threshold = Config.Bind("Configure /Prioritize by Haulers/",
                                             "threshold",
                                             0.25f,
                                             new ConfigDescription($"Threshold (%full input/%full output) for applying the prioritization ... ",
                                             new AcceptableValueRange<float>(0f, 1f))
                                             );
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
                if (____haulPrioritizable.Prioritized && (__result >= EP.Prioritize_threshold))
                {
                    __result += EP.Prioritize_strength;
                }
                return false;
            }
        }
    }
}