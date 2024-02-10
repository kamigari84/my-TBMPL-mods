using Bindito.Core;
using System.Collections.Generic;
using Timberborn.Attractions;
using Timberborn.BaseComponentSystem;
using Timberborn.BuildingsBlocking;
using Timberborn.Emptying;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Hauling;
using Timberborn.InventorySystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using UnityDev.Utils.LogUtilsLite;

namespace NoWorkerHaul.HaulBehaviourProvider.Core
{

    public class ManufactoryHaul : BaseComponent, IHaulBehaviorProvider
    {
        private InventoryFillCalculator _inventoryFillCalculator;

        private Manufactory _manufactory;

        private BlockableBuilding _blockableBuilding;

        private Inventories _inventories;

        private FillInputWorkplaceBehavior _fillInputWorkplaceBehavior;

        private EmptyOutputWorkplaceBehavior _emptyOutputWorkplaceBehavior;
        public bool NoWorkersManufactory { get; protected set; }
        public float FillingThreshold { get; set; } = 0f;
        public float EmptyingThreshold { get; set; } = 0.34f;
        public float NoWorkerBonus { get; set; } = 0.8f;

        [Inject]
        public void InjectDependencies(InventoryFillCalculator inventoryFillCalculator)
        {
            _inventoryFillCalculator = inventoryFillCalculator;
        }

        public void Awake()
        {
            _manufactory = GetComponentFast<Manufactory>();
            _blockableBuilding = GetComponentFast<BlockableBuilding>();
            _inventories = GetComponentFast<Inventories>();
            _fillInputWorkplaceBehavior = GetComponentFast<FillInputWorkplaceBehavior>();
            _emptyOutputWorkplaceBehavior = GetComponentFast<EmptyOutputWorkplaceBehavior>();
            NoWorkersManufactory = GetComponentFast<ProductionIncreaser>() != null;
            FillingThreshold = EP.FillingThreshold;
            EmptyingThreshold = EP.EmptyingThreshold;
            NoWorkerBonus = EP.Workerless_floor;
        }

        public void GetWeightedBehaviors(IList<WeightedBehavior> weightedBehaviors)
        {
            DebugEx.Fine($"{name} GetWeightedBehaviors \n\t Fill threshold: {FillingThreshold} \n\t Empty threshold: {EmptyingThreshold}");
            if (_manufactory && _manufactory.HasCurrentRecipe && _blockableBuilding.IsUnblocked)
            {
                foreach (Inventory enabledInventory in _inventories.EnabledInventories)
                {
                    if (enabledInventory.IsInput)
                    {
                        DebugEx.Fine("Calculating input haul-priority");
                        float weight = 1f - _inventoryFillCalculator.GetInputFillPercentage(enabledInventory);
                        if (weight > 0f)
                        {
                            if (NoWorkersManufactory && weight >= FillingThreshold)
                            {
                                DebugEx.Fine($"Hauling priority for NoWorker manufacturer increased by {NoWorkerBonus}");
                                weight += NoWorkerBonus;
                            }

                            weightedBehaviors.Add(new WeightedBehavior(weight, _fillInputWorkplaceBehavior));
                        }
                    }
                    if (enabledInventory.IsOutput)
                    {
                        DebugEx.Fine($"Calculating output haul-priority");

                        float weight = _inventoryFillCalculator.GetOutputFillPercentage(enabledInventory);
                        if (weight > 0f)
                        {
                            if (NoWorkersManufactory && weight >= EmptyingThreshold)
                            {
                                DebugEx.Fine($"Hauling priority for NoWorker manufacturer increased by {NoWorkerBonus}");
                                weight += NoWorkerBonus;
                            }

                            weightedBehaviors.Add(new WeightedBehavior(weight, _emptyOutputWorkplaceBehavior));
                        }
                    }
                }
            }
        }
    }

    public class FillInput : BaseComponent, IHaulBehaviorProvider
    {
        private InventoryFillCalculator _inventoryFillCalculator;

        private BlockableBuilding _blockableBuilding;

        private Inventories _inventories;

        private FillInputWorkplaceBehavior _fillInputWorkplaceBehavior;
        public bool NoWorkersManufactory { get; protected set; }

        [Inject]
        public void InjectDependencies(InventoryFillCalculator inventoryFillCalculator)
        {
            _inventoryFillCalculator = inventoryFillCalculator;
        }

        public void Awake()
        {
            _blockableBuilding = GetComponentFast<BlockableBuilding>();
            _inventories = GetComponentFast<Inventories>();
            _fillInputWorkplaceBehavior = GetComponentFast<FillInputWorkplaceBehavior>();
            NoWorkersManufactory = GetComponentFast<ProductionIncreaser>() != null || (GetComponentFast<GoodConsumingBuilding>() != null && GetComponentFast<Workplace>() == null) || GetComponentFast<GoodConsumingAttraction>() != null;
        }

        public void GetWeightedBehaviors(IList<WeightedBehavior> weightedBehaviors)
        {
            DebugEx.Info($"Calculating input-only haul-priority for {name}");

            foreach (Inventory enabledInventory in _inventories.EnabledInventories)
                {
                    if (_blockableBuilding.IsUnblocked)
                    {
                    DebugEx.Info($"{name} is not blocked");
                    float weight = 1f - _inventoryFillCalculator.GetInputFillPercentage(enabledInventory);
                        if (weight > 0f)
                        {
                            if (NoWorkersManufactory && weight >= EP.FillingThreshold)
                            {
                                weight += EP.Workerless_floor;
                            }

                            weightedBehaviors.Add(new WeightedBehavior(weight, _fillInputWorkplaceBehavior));

                        }
                    }
                }
        }
    }
}