// Timberborn Mod: SmartPower
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;
using TimberApi.ConfiguratorSystem;
using TimberApi.SceneSystem;
using Timberborn.Workshops;
using Bindito.Unity;
using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using Timberborn.BaseComponentSystem;
using HaulBehaviorProvider_for_Workerless_Manufactory;
using Timberborn.WorkSystem;
using HaulBehaviorProvider_for_Workerless_Manufactory.HaulBehaviourProvider.Core;
using UnityDev.Utils.LogUtilsLite;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace IgorZ.SmarterHaulers
{

    [Configurator(SceneEntrypoint.All)]
    // ReSharper disable once UnusedType.Global
    sealed class Configurator : IConfigurator
    {
        static readonly string PatchId = typeof(Configurator).FullName;

        public void Configure(IContainerDefinition containerDefinition) { }
        [HarmonyPatch(typeof(Instantiator), nameof(Instantiator.InstantiateInactive))]
        static class PrefabInstantiatePatch
        {

            static void Prefix(GameObject prefab)
            {
                var components = prefab.GetComponents<BaseComponent>().Select(x => x.GetType()).ToArray();
                if (components.Contains(typeof(Manufactory)))
                {
                    if (prefab.GetComponent<Manufactory>() != null)
                    {
                        /*if (prefab.GetComponent<ManufactoryHaulBehaviorProvider>() != null)
                        {
                            UnityEngine.Object.DestroyImmediate(prefab.GetComponent<ManufactoryHaulBehaviorProvider>());
                            DebugEx.Info(format: $"removed ManufactoryHaulBehaviorProvider from Manufactory {prefab.name}");
                        }*/
                        if (prefab.GetComponent<ManufactoryHaul>() == null)
                        {
                            prefab.AddComponent<ManufactoryHaul>();
                            DebugEx.Info(format: $"added missing ManufactoryHaul to Manufactory {prefab.name}");

                        }
                        /*if (components.Contains(typeof(FillInputHaulBehaviorProvider)))
                        {
                            if (prefab.GetComponent<FillInput>() == null && prefab.GetComponent<FillInputHaulBehaviorProvider>() != null)
                            {
                                //DebugEx.Fine("Replace component on prefab: name={0}, oldComponent={1}, newComponent={2}",
                                //prefab.name, typeof(FillInputHaulBehaviorProvider), typeof(NoWorkersFillInputHaulBehaviorProvider));
                                UnityEngine.Object.DestroyImmediate(prefab.GetComponent<FillInputHaulBehaviorProvider>());
                                prefab.AddComponent<FillInput>();
                            }
                        }*/
                        if (ModEntry.Config.RemoveUnneededWorkplaces && components.Contains(typeof(ProductionIncreaser)) && components.Contains(typeof(Workshop)))
                        {
                            if (prefab.GetComponent<Workshop>() != null)
                            {
                                DebugEx.Info(format: $"Remove component on prefab: name={prefab.name}, removedComponent={typeof(Workshop).FullName}");
                                UnityEngine.Object.DestroyImmediate(prefab.GetComponent<Workshop>());
                            }
                        }


                    }
                }
            }
        }
    }
}

