using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoR2;

namespace RecipeHelper
{
    // What the CHEF could consume, as pickup counts. Immutable: Apply returns the state after a craft.
    internal sealed class InventorySnapshot
    {
        private readonly Dictionary<PickupIndex, int> counts;

        public InventorySnapshot(Dictionary<PickupIndex, int> counts)
        {
            this.counts = counts;
        }

        public int Count(PickupIndex pickup)
        {
            int count;
            return counts.TryGetValue(pickup, out count) ? count : 0;
        }

        public bool IsEmpty => counts.Count == 0;

        // Mirrors CraftingController.GetGeneratedOptionsFromInteractor: the held equipment, plus permanent items the
        // CHEF allows. Temporary items are not counted.
        public static InventorySnapshot FromInventory(Inventory inventory)
        {
            Dictionary<PickupIndex, int> counts = new Dictionary<PickupIndex, int>();
            if (!inventory)
            {
                return new InventorySnapshot(counts);
            }

            EquipmentIndex equipment = inventory.GetEquipmentIndex();
            if (equipment != EquipmentIndex.None)
            {
                counts[PickupCatalog.FindPickupIndex(equipment)] = 1;
            }

            foreach (ItemIndex itemIndex in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                int count = inventory.GetItemCountPermanent(itemIndex);
                if (itemDef && ((itemDef.canRemove && !itemDef.hidden && count > 0)
                    || itemDef.ContainsTag(ItemTag.AllowedForUseAsCraftingIngredient)))
                {
                    if (count > 0)
                    {
                        counts[PickupCatalog.FindPickupIndex(itemIndex)] = count;
                    }
                }
            }
            return new InventorySnapshot(counts);
        }

        public InventorySnapshot Apply(CraftStep step)
        {
            Dictionary<PickupIndex, int> next = new Dictionary<PickupIndex, int>(counts);
            foreach (PickupIndex ingredient in step.Ingredients)
            {
                int left = next[ingredient] - 1;
                if (left > 0)
                {
                    next[ingredient] = left;
                }
                else
                {
                    next.Remove(ingredient);
                }
            }

            PickupDef result = PickupCatalog.GetPickupDef(step.Result);
            if (result != null && result.equipmentIndex != EquipmentIndex.None)
            {
                // A character holds one equipment; the game swaps it out rather than consuming it.
                foreach (PickupIndex held in next.Keys.ToList())
                {
                    PickupDef heldDef = PickupCatalog.GetPickupDef(held);
                    if (heldDef != null && heldDef.equipmentIndex != EquipmentIndex.None)
                    {
                        next.Remove(held);
                    }
                }
                next[step.Result] = 1;
            }
            else
            {
                int existing;
                next.TryGetValue(step.Result, out existing);
                next[step.Result] = existing + step.Amount;
            }
            return new InventorySnapshot(next);
        }

        // Order-independent identity, so two craft orders reaching the same inventory are searched once.
        public string Key()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<PickupIndex, int> pair in counts.OrderBy(p => p.Key.value))
            {
                builder.Append(pair.Key.value).Append(':').Append(pair.Value).Append(',');
            }
            return builder.ToString();
        }

        public override string ToString()
        {
            return string.Join(", ", counts.Select(p => PickupCatalog.GetPickupDef(p.Key)?.internalName + " x" + p.Value));
        }
    }
}
