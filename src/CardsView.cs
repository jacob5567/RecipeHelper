using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RecipeHelper
{
    // Sidebar style A: one card per result, with each craft drawn as icons and the other held items that fit a slot
    // shown under it.
    internal static class CardsView
    {
        private const float ResultSize = 56f;
        private const float SlotSize = 38f;
        private const float AlternateSize = 22f;
        private const int MaxAlternates = 4;

        public static List<SidebarBlock> Blocks(List<CraftPlan> plans, InventorySnapshot start)
        {
            List<SidebarBlock> blocks = new List<SidebarBlock>();
            foreach (CraftPlan plan in plans)
            {
                CraftPlan captured = plan;
                blocks.Add(new SidebarBlock
                {
                    Group = GroupLabel(plan.Steps.Count),
                    Results = 1,
                    Build = (parent, width) => Card(parent, captured, StepDetail.Build(start, captured)),
                });
            }
            return blocks;
        }

        public static string GroupLabel(int steps)
        {
            return steps == 1 ? "NOW" : "IN " + steps + " CRAFTS";
        }

        private static void Card(RectTransform parent, CraftPlan plan, List<StepDetail> details)
        {
            RectTransform card = Ui.Row(parent, "Card", 12f);
            Ui.Pad(card, 10);
            Ui.Background(card, Ui.CardBackground);
            Ui.Tile(card, plan.Result, ResultSize);

            RectTransform body = Ui.Stack(card, "Body", 8f);
            Ui.Flexible(body);
            int amount = plan.Steps[plan.Steps.Count - 1].Amount;
            Ui.Text(body, Ui.Name(plan.Result) + (amount > 1 ? " x" + amount : ""), 17f, Ui.TierColor(plan.Result));

            for (int n = 0; n < details.Count; n++)
            {
                RectTransform row = Ui.Row(body, "Step", 6f);
                if (details.Count > 1)
                {
                    Ui.Size(Ui.Text(row, (n + 1) + ".", 12f, Ui.Muted).rectTransform, 16f, SlotSize);
                }
                SlotDetail[] slots = details[n].Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i > 0)
                    {
                        Ui.Glyph(row, "+", SlotSize);
                    }
                    Slot(row, slots[i]);
                }
                Ui.Glyph(row, "→", SlotSize);
                Ui.Tile(row, details[n].Step.Result, SlotSize);
            }
        }

        private static void Slot(RectTransform row, SlotDetail slot)
        {
            RectTransform column = Ui.Stack(row, "Slot", 3f, TextAnchor.UpperCenter);
            Ui.Tile(column, slot.Chosen, SlotSize, slot.MadeByEarlierStep);
            if (slot.Alternates.Count == 0)
            {
                return;
            }
            Ui.Text(column, "or", 11f, Ui.Muted);
            RectTransform alternates = Ui.Row(column, "Alternates", 3f);
            for (int i = 0; i < slot.Alternates.Count && i < MaxAlternates; i++)
            {
                Ui.Tile(alternates, slot.Alternates[i], AlternateSize);
            }
            if (slot.Alternates.Count > MaxAlternates)
            {
                Ui.Text(alternates, "+" + (slot.Alternates.Count - MaxAlternates), 11f, Ui.Muted);
            }
        }
    }
}
