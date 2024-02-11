using System;
using System.IO;
using System.Reflection;
using File = System.IO.File;

namespace Helpers
{
    public static class EPHelpers
    {

        public static void TAPI_declarer(string GUID,
                                         string Version,
                                         string Desc,
                                         string MinAPIVer = "0.6.5",
                                         string MinExeVer = "0.5.7")
        {
            var plugin_dll = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var mod_declaration = Path.Combine(Path.GetDirectoryName(plugin_dll), "mod.json");
            if (!File.Exists(mod_declaration))
            {

                File.WriteAllText(mod_declaration,
                                  $"{{\r\n  \"Name\": \"{Desc}/\",                     // Name of the mod\r\n" +
                                  $"  \"Version\": \"{Version}\",                       // Version of the mod\r\n" +
                                  $"  \"UniqueId\": \"{GUID}\",     // Unique identifier of the mod\r\n" +
                                  $"  \"MinimumApiVersion\": \"{MinAPIVer}\",             // Minimun TimberAPI version this mod needs\r\n" +
                                  $"  \"MinimumGameVersion\": \"{MinExeVer}\",            // Minimun game version this mod needs (0.2.8 is the lowest that works with TimberAPI v0.5)\r\n" +
                                  $"  \"EntryDll\": \"{Path.GetFileName(plugin_dll)}\", // Optional. The entry dll if the mod has custom code\r\n" +
                                  $"  \"Assets\": [                               // Optional. The Prefix for the asset bundle and the scenes where they should be loaded. \r\n" +
                                  $"    {{\r\n      \"Prefix\": \"{GUID}\",\r\n      \"Scenes\": [\r\n        \"All\"\r\n      ]\r\n    }}\r\n  ]\r\n}}");
            }
        }
    }
}