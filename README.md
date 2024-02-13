# TBMPL_mods
 Some code/config-only mods for Timberborn using TAPI/BepInEx (originally used TBMPL)

[Fix Workerless Recipes]

Soon-to-be-deprecated mod. Adds config for "Prioritize by haulers" & configurable tweaks to hauling priority for workerless buildings (good-consuming/producing, that is)
Originally depended on TBMPL, now depends on TAPI instead. Uses BepInEx config.
It "Just Works*™ but...


[HaulBehaviorProvider for Workerless Manufactory]

New/split-off mod. Makes tweaks for good-consuming/producing No-Worker-Needed buildings, by replacing the HaulBehaviorProviders for manufactory & good-consuming buildings.
TAPI mod with BepInEx config.
Still WIP, dll currently here is broken.


[PrioritizeByHaulers_Configuration]

New/split-off mod. Simply adds configuration for "Prioritize by haulers".
TAPI mod with BepInEx config.
Works well, and smoother.

[WaterPumpPipe Extension]

TBMPL mod. Lets water intake pipes have greater max length/depth, with configurable addend/multiplier.
You should use [Wireless Pumps](https://mod.io/g/timberborn/m/wireless-pumps) instead


[Helpers]

 Not a mod; not even a project.
 It's just some helpers, most from [Igorz](https://github.com/ihsoft/TimberbornMods) - and my TAPI mod.json autogenerator (start the game once, close & reopen ... voila, TAPI now recognises the dll-only mod)

 [External Utilities]
 I would reccomend using [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) - It is a good way to change BepInEx style configs ingame. However, on Timberborn it's letting all inputs through to the game.


NOTE: Hauling Patch (at least <Fix Workerless Recipes>), for reason unknown, breaks saves with any Gathering Prioritizations upon un/install
