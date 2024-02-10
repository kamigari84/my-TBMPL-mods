using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BuildingsBlocking;
using Timberborn.Hauling;
using Timberborn.InventorySystem;
using Timberborn.Workshops;

namespace AlwaysFill
{
    public class AlwaysFillInput : BaseComponent, IHaulBehaviorProvider
    {
        private BlockableBuilding _blockableBuilding;

        private Inventories _inventories;

        private FillInputWorkplaceBehavior _fillInputWorkplaceBehavior;
        private float weight = 1.1f;

    public void Awake()
        {
            _blockableBuilding = GetComponentFast<BlockableBuilding>();
            _inventories = GetComponentFast<Inventories>();
            _fillInputWorkplaceBehavior = GetComponentFast<FillInputWorkplaceBehavior>();
        }

        public void GetWeightedBehaviors(IList<WeightedBehavior> weightedBehaviors)
        {
            foreach (var inv in _inventories._enabledInventories)
            {
                if (_blockableBuilding.IsUnblocked)
                {
                    if (inv.TotalAmountInStock < inv.Capacity)
                    {
                        weightedBehaviors.Add(new WeightedBehavior(weight, _fillInputWorkplaceBehavior));
                    }
                }
            }

        }
    }
}