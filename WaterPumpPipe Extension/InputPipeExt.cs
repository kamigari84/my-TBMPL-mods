using TBMPLCore.Plugin;
using TBMPLCore.Plugin.Attributes;
using TBMPLCore.Plugin.Config;
using HarmonyLib;
using Timberborn.WaterBuildings;


namespace WaterPumpPipe_Extension
{
    [TBMPL(TBMPL.Prefix + "PumpPipeExt", "WaterPumpPipe Extension", "1.0.1")]
    internal sealed class EP : EntryPoint
    {
        public static new EPConfig Config { get; }

        // Creates the plugin configuration
        protected override IConfig GetConfig()
        {
            return new EPConfig
            {
                Multiplier = AddKey("modifiers", "Multiplier", 1, "Max Pipe Length Multiplier"), // int default value 1
                Addend = AddKey("modifiers", "Addend", 0, "Max Pipe Lenght Addendum"), // int default value 0
            };
        }

    }
    internal sealed class EPConfig : BaseConfig
    {
        public int Multiplier { get; set; }
        public int Addend { get; set; }
    }


    // Some of our patches for the game
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaterInputSpecification), "Awake")]
        private static bool DepthPatcher(ref int ____maxDepth)
        {
            ____maxDepth = ____maxDepth * EP.Config.Multiplier + EP.Config.Addend;
            return false; // make sure the original doesn't run
        }

    }


}
