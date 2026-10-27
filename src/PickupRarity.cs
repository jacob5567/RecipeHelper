using RoR2;

namespace RecipeHelper
{
    // Orders results rarest first, with meals on top since only the CHEF makes them. Void items rank with the tier they
    // corrupt; lunar and boss equipment rank with their item counterparts, and ordinary equipment sits between green
    // and red.
    internal static class PickupRarity
    {
        public static int Rank(PickupIndex index)
        {
            PickupDef pickup = PickupCatalog.GetPickupDef(index);
            if (pickup == null)
            {
                return 0;
            }
            if (pickup.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickup.itemIndex);
                return item ? ItemRank(item.tier) : 0;
            }
            if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                if (!equipment) return 0;
                if (equipment.isBoss) return ItemRank(ItemTier.Boss);
                if (equipment.isLunar) return ItemRank(ItemTier.Lunar);
                return 4;
            }
            return 0;
        }

        private static int ItemRank(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.FoodTier:
                    return 9;
                case ItemTier.Tier3:
                case ItemTier.VoidTier3:
                    return 8;
                case ItemTier.Boss:
                case ItemTier.VoidBoss:
                    return 7;
                case ItemTier.Lunar:
                    return 6;
                case ItemTier.Tier2:
                case ItemTier.VoidTier2:
                    return 3;
                case ItemTier.Tier1:
                case ItemTier.VoidTier1:
                    return 2;
                default:
                    return 1;
            }
        }
    }
}
