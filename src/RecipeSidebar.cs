using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecipeHelper
{
    // Lives on the CHEF's CraftingPanel, so it goes away with the dialog. Lists what the participant's inventory can
    // craft, recomputed only when the inventory changes.
    //
    // Read-only: never call the CraftingController's ClearSlot, ClearAllSlots, SendToSlot, ConfirmSelection,
    // ConfirmButtonHit, CancelButtonHit, OnConfirmServer or SendToMealprep, or set its ingredients. They change the
    // dialog's state or send messages to the host.
    internal sealed class RecipeSidebar : MonoBehaviour
    {
        private const int Padding = 10;
        private const float Spacing = 6f;
        private const float HeaderFontSize = 24f;
        private const float BodyFontSize = 18f;
        private const float Margin = 24f;
        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color BodyColor = new Color32(0xc3, 0xc7, 0xd3, 0xff);

        private CraftingPanel panel;
        private HUD hud;
        private Inventory inventory;
        private GameObject root;
        private TextMeshProUGUI header;
        private TextMeshProUGUI body;
        private bool rendered;

        // Read by the RecipeHelper.Tests plugin.
        internal List<CraftPlan> Plans { get; private set; } = new List<CraftPlan>();

        public void Init(HUD owner)
        {
            hud = owner;
            panel = GetComponent<CraftingPanel>();
            BuildUi();
        }

        private void Update()
        {
            // The participant is synced after the panel appears, so the inventory is picked up here rather than in Init.
            CharacterMaster master = panel.craftingController?.networkUIPromptController?.currentParticipantMaster;
            Inventory current = master ? master.inventory : null;
            if (rendered && current == inventory)
            {
                return;
            }
            if (inventory)
            {
                inventory.onInventoryChanged -= Refresh;
            }
            inventory = current;
            if (inventory)
            {
                inventory.onInventoryChanged += Refresh;
            }
            rendered = true;
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory)
            {
                inventory.onInventoryChanged -= Refresh;
            }
            if (root)
            {
                Destroy(root);
            }
        }

        private void Refresh()
        {
            PluginConfig settings = RecipeHelperPlugin.Settings;
            RecipePlanner planner = RecipeBook.Planner;
            InventorySnapshot snapshot = settings.DebugAlwaysShow.Value ? EverythingSnapshot() : InventorySnapshot.FromInventory(inventory);
            int steps = settings.DebugAlwaysShow.Value ? 1 : settings.MaxChainSteps.Value;
            Plans = planner.Plan(snapshot, steps, RecipePlannerLimits.StateBudget);
            if (planner.HitBudget && settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo("Recipe search stopped at the state budget; longer chains may be missing.");
            }
            Render(planner.HitBudget);
        }

        private static InventorySnapshot EverythingSnapshot()
        {
            Dictionary<PickupIndex, int> all = new Dictionary<PickupIndex, int>();
            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                all[pickup.pickupIndex] = 2;
            }
            return new InventorySnapshot(all);
        }

        private void Render(bool truncatedSearch)
        {
            int maxRows = RecipeHelperPlugin.Settings.MaxRows.Value;
            header.text = Plans.Count == 0 ? "Nothing to craft" : "You can craft";
            StringBuilder text = new StringBuilder();
            int shown = 0;
            int lastSteps = 0;
            foreach (CraftPlan plan in Plans)
            {
                if (shown >= maxRows)
                {
                    break;
                }
                if (plan.Steps.Count != lastSteps)
                {
                    lastSteps = plan.Steps.Count;
                    text.Append("\n<size=85%><color=#8fa3c7>").Append(lastSteps == 1 ? "Now" : "In " + lastSteps + " crafts").Append("</color></size>\n");
                }
                text.Append(Name(plan.Result, plan.Steps[plan.Steps.Count - 1].Amount)).Append('\n');
                foreach (CraftStep step in plan.Steps)
                {
                    text.Append("  <size=85%>").Append(StepLine(step)).Append("</size>\n");
                }
                shown++;
            }
            if (Plans.Count > shown)
            {
                text.Append("\n<size=85%>and ").Append(Plans.Count - shown).Append(" more</size>");
            }
            if (truncatedSearch)
            {
                text.Append("\n<size=85%>Longer chains not searched.</size>");
            }
            body.text = text.ToString();
        }

        private static string StepLine(CraftStep step)
        {
            List<string> ingredients = new List<string>();
            foreach (PickupIndex ingredient in step.Ingredients)
            {
                ingredients.Add(Name(ingredient, 1));
            }
            return string.Join(" + ", ingredients) + " → " + Name(step.Result, step.Amount);
        }

        private static string Name(PickupIndex index, int amount)
        {
            PickupDef def = PickupCatalog.GetPickupDef(index);
            if (def == null)
            {
                return "?";
            }
            string name = Language.GetString(def.nameToken);
            string suffix = amount > 1 ? " x" + amount : "";
            return "<color=#" + ColorUtility.ToHtmlStringRGB(def.baseColor) + ">" + name + "</color>" + suffix;
        }

        private void BuildUi()
        {
            float width = RecipeHelperPlugin.Settings.SidebarWidth.Value;
            root = new GameObject("RecipeHelperSidebar", typeof(RectTransform));
            Transform parent = hud.mainContainer.transform;
            root.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-Margin, 0f);
            rect.sizeDelta = new Vector2(width, 0f);

            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.AddComponent<Image>().color = BackgroundColor;
            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            header = CreateText(rect, "Header", HeaderFontSize, Color.white);
            body = CreateText(rect, "Body", BodyFontSize, BodyColor);
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, float fontSize, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            HGTextMeshProUGUI text = textObject.AddComponent<HGTextMeshProUGUI>();
            text.richText = true;
            text.enableWordWrapping = true;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }

    internal static class RecipePlannerLimits
    {
        // Caps the search so a large inventory can't stall the dialog.
        public const int StateBudget = 20000;
    }
}
