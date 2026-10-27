using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.UI;

namespace RecipeHelper
{
    // Sidebar style C: each group's results as a grid of icons, and a panel showing how to make the selected one, with
    // every held item that fits each slot.
    internal static class GridView
    {
        private const float CellSize = 54f;
        private const float CellSpacing = 8f;
        private const float RingWidth = 3f;
        private const float DetailResultSize = 56f;
        private const float OptionSize = 40f;
        private const float AlternateSize = 26f;
        private const float SlotWidth = 110f;
        private const int MaxAlternates = 4;
        private static readonly Color SelectedRing = Color.white;
        private static readonly Color UnselectedRing = new Color(1f, 1f, 1f, 0f);

        public static List<SidebarBlock> Blocks(List<CraftPlan> plans, InventorySnapshot start, PickupIndex selected, Action<PickupIndex> select)
        {
            List<SidebarBlock> blocks = new List<SidebarBlock>();
            int i = 0;
            while (i < plans.Count)
            {
                int steps = plans[i].Steps.Count;
                List<CraftPlan> group = new List<CraftPlan>();
                for (; i < plans.Count && plans[i].Steps.Count == steps; i++)
                {
                    group.Add(plans[i]);
                }
                blocks.Add(new SidebarBlock
                {
                    Results = group.Count,
                    Build = (parent, width) => Group(parent, width, steps, group, selected, select),
                });
            }

            CraftPlan shown = plans.Find(p => p.Result == selected) ?? plans[0];
            blocks.Add(new SidebarBlock
            {
                Required = true,
                Build = (parent, width) => Detail(parent, shown, StepDetail.Build(start, shown)),
            });
            return blocks;
        }

        private static void Group(RectTransform parent, float width, int steps, List<CraftPlan> plans, PickupIndex selected, Action<PickupIndex> select)
        {
            RectTransform stack = Ui.Stack(parent, "Group", 8f);
            Ui.Text(stack, CardsView.GroupLabel(steps), 13f, Ui.Muted);

            RectTransform gridRect = Ui.Rect(stack, "Grid");
            GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellSize, CellSize);
            grid.spacing = new Vector2(CellSpacing, CellSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((width + CellSpacing) / (CellSize + CellSpacing)));

            foreach (CraftPlan plan in plans)
            {
                PickupIndex result = plan.Result;
                RectTransform cell = Ui.Rect(gridRect, "Result");
                Image ring = cell.gameObject.AddComponent<Image>();
                ring.color = result == selected ? SelectedRing : UnselectedRing;
                Ui.Fill(Ui.Tile(cell, result, CellSize - 2f * RingWidth), RingWidth);

                // Local only: picking a result changes what this sidebar shows and nothing else.
                Button button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = ring;
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => select(result));
            }
        }

        private static void Detail(RectTransform parent, CraftPlan plan, List<StepDetail> details)
        {
            RectTransform panel = Ui.Stack(parent, "Detail", 12f);
            Ui.Pad(panel, 12);
            Ui.Background(panel, Ui.CardBackground);

            RectTransform title = Ui.Row(panel, "Title", 12f);
            Ui.Tile(title, plan.Result, DetailResultSize);
            RectTransform names = Ui.Stack(title, "Names", 2f);
            Ui.Flexible(names);
            int amount = plan.Steps[plan.Steps.Count - 1].Amount;
            Ui.Text(names, Ui.Name(plan.Result) + (amount > 1 ? " x" + amount : ""), 19f, Ui.TierColor(plan.Result));
            Ui.Text(names, details.Count == 1 ? "One craft" : details.Count + " crafts", 12f, Ui.Muted);

            for (int n = 0; n < details.Count; n++)
            {
                RectTransform row = Ui.Row(panel, "Step", 8f);
                Ui.Size(Ui.Text(row, (n + 1) + ".", 12f, Ui.Muted).rectTransform, 18f, OptionSize);
                SlotDetail[] slots = details[n].Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i > 0)
                    {
                        Ui.Glyph(row, "+", OptionSize);
                    }
                    Slot(row, slots[i]);
                }
                Ui.Glyph(row, "→", OptionSize);
                Ui.Tile(row, details[n].Step.Result, OptionSize);
            }
        }

        // The pickup the plan uses, then the other held pickups that fit, each with its name.
        private static void Slot(RectTransform row, SlotDetail slot)
        {
            RectTransform column = Ui.Stack(row, "Slot", 4f);
            Ui.Width(column, SlotWidth);
            Option(column, slot.Chosen, OptionSize, 12f, Ui.TierColor(slot.Chosen), slot.MadeByEarlierStep);
            for (int i = 0; i < slot.Alternates.Count && i < MaxAlternates; i++)
            {
                Option(column, slot.Alternates[i], AlternateSize, 11f, Ui.Muted, false);
            }
            if (slot.Alternates.Count > MaxAlternates)
            {
                Ui.Text(column, "+" + (slot.Alternates.Count - MaxAlternates) + " more", 11f, Ui.Muted);
            }
        }

        private static void Option(RectTransform column, PickupIndex pickup, float size, float fontSize, Color color, bool madeByEarlierStep)
        {
            RectTransform option = Ui.Row(column, "Option", 6f);
            option.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Ui.Tile(option, pickup, size, madeByEarlierStep);
            Ui.Flexible(Ui.Text(option, Ui.Name(pickup), fontSize, color).rectTransform);
        }
    }
}
