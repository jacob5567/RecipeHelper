using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecipeHelper
{
    // Sidebar style E: one row per result showing its crafts. Clicking a row puts the first craft in the CHEF's slots;
    // the later crafts of a chain are dimmed, since they can only be made at a later visit.
    internal static class ListView
    {
        public const string Hint = "Click a row to put its first craft in the slots.";
        private const float ResultSize = 44f;
        private const float NextSize = 24f;
        private const int MaxAlternateNames = 3;
        private const float LaterStepAlpha = 0.55f;
        private static readonly Color RowHover = new Color(1f, 1f, 1f, 0.09f);
        private static readonly Color RowInSlots = new Color32(0x8f, 0xa3, 0xc7, 0x29);
        private static readonly Color BadgeInSlots = new Color32(0xc3, 0xd2, 0xee, 0xff);
        private static readonly Color BadgeDefault = new Color32(0x8f, 0xa3, 0xc7, 0x26);
        private static readonly Color BadgeInSlotsText = new Color32(0x0b, 0x0e, 0x14, 0xff);

        public static List<SidebarBlock> Blocks(List<CraftPlan> plans, InventorySnapshot start, Func<CraftStep, bool> inSlots, Action<CraftStep> fill)
        {
            List<SidebarBlock> blocks = new List<SidebarBlock>
            {
                new SidebarBlock { Build = (parent, width) => Ui.Text(parent, Hint, 12f, Ui.Muted) },
            };
            foreach (CraftPlan plan in plans)
            {
                CraftPlan captured = plan;
                blocks.Add(new SidebarBlock
                {
                    Group = CardsView.GroupLabel(plan.Steps.Count),
                    Results = 1,
                    Build = (parent, width) => Row(parent, captured, StepDetail.Build(start, captured), inSlots(captured.Steps[0]), fill),
                });
            }
            return blocks;
        }

        private static void Row(RectTransform parent, CraftPlan plan, List<StepDetail> details, bool inSlots, Action<CraftStep> fill)
        {
            RectTransform row = Ui.Row(parent, "Recipe", 12f);
            Ui.Pad(row, 8);
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Image background = row.gameObject.AddComponent<Image>();
            Color normal = inSlots ? RowInSlots : Ui.CardBackground;
            background.color = normal;
            Ui.Tile(row, plan.Result, ResultSize);

            RectTransform body = Ui.Stack(row, "Body", 5f);
            Ui.Flexible(body);
            int amount = plan.Steps[plan.Steps.Count - 1].Amount;
            Ui.Text(body, Ui.Name(plan.Result) + (amount > 1 ? " x" + amount : ""), 16f, Ui.TierColor(plan.Result));

            for (int n = 0; n < details.Count; n++)
            {
                Craft(body, details[n], details.Count > 1 ? n + 1 : 0);
                // Only the first craft can be put in the slots, so only its alternatives are listed.
                string alternates = n == 0 ? AlternateNames(details[0]) : null;
                if (alternates != null)
                {
                    Ui.Text(body, alternates, 11f, Ui.Muted);
                }
            }

            Badge(row, inSlots ? "In slots" : plan.Steps.Count == 1 ? "1 craft" : plan.Steps.Count + " crafts", inSlots);

            // Local until clicked: the click sends the same picks as clicking the items in the dialog. See SlotFiller.
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            CraftStep step = plan.Steps[0];
            button.onClick.AddListener(() => fill(step));
            HoverColor hover = row.gameObject.AddComponent<HoverColor>();
            hover.Target = background;
            hover.Normal = normal;
            hover.Hover = inSlots ? normal : RowHover;
        }

        // One craft as small icons, numbered when the result takes several. Crafts after the first are dimmed.
        private static void Craft(RectTransform body, StepDetail detail, int number)
        {
            RectTransform craft = Ui.Row(body, "Craft", 4f);
            craft.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            if (number > 0)
            {
                Ui.Size(Ui.Text(craft, number + ".", 11f, Ui.Muted).rectTransform, 14f, NextSize);
            }
            if (number > 1)
            {
                craft.gameObject.AddComponent<CanvasGroup>().alpha = LaterStepAlpha;
            }
            for (int i = 0; i < detail.Slots.Length; i++)
            {
                if (i > 0)
                {
                    Ui.Glyph(craft, "+", NextSize);
                }
                Ui.Tile(craft, detail.Slots[i].Chosen, NextSize, detail.Slots[i].MadeByEarlierStep);
            }
            Ui.Glyph(craft, "→", NextSize);
            Ui.Tile(craft, detail.Step.Result, NextSize);
        }

        // "or A, B" for the other held items that fit the next craft's slots.
        private static string AlternateNames(StepDetail next)
        {
            List<string> names = new List<string>();
            foreach (SlotDetail slot in next.Slots)
            {
                foreach (PickupIndex alternate in slot.Alternates)
                {
                    names.Add(Ui.Name(alternate));
                }
            }
            if (names.Count == 0)
            {
                return null;
            }
            string shown = string.Join(", ", names.GetRange(0, Math.Min(names.Count, MaxAlternateNames)));
            return "or " + shown + (names.Count > MaxAlternateNames ? " +" + (names.Count - MaxAlternateNames) + " more" : "");
        }

        private static void Badge(RectTransform row, string text, bool inSlots)
        {
            RectTransform badge = Ui.Row(row, "Badge", 0f);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 3, 3);
            Ui.Background(badge, inSlots ? BadgeInSlots : BadgeDefault);
            Ui.Text(badge, text, 11f, inSlots ? BadgeInSlotsText : Ui.Muted).enableWordWrapping = false;
        }
    }

    // Lightens a row while the pointer is over it.
    internal sealed class HoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Target;
        public Color Normal;
        public Color Hover;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Target.color = Hover;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Target.color = Normal;
        }
    }
}
