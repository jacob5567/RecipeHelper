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
        private bool loggedLayout;
        private bool clamped;

        // Read by the RecipeHelper.Tests plugin.
        internal List<CraftPlan> Plans { get; private set; } = new List<CraftPlan>();

        public void Init(HUD owner)
        {
            hud = owner;
            panel = GetComponent<CraftingPanel>();
            BuildUi();
            if (RecipeHelperPlugin.Settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo($"Sidebar: built on '{name}', parent '{root.transform.parent.name}'");
            }
        }

        private void Update()
        {
            if (!clamped && rendered)
            {
                clamped = true;
                ClampToScreen();
            }
            if (!loggedLayout && rendered && RecipeHelperPlugin.Settings.DebugLogging.Value)
            {
                loggedLayout = true;
                LogLayout();
            }
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
            if (settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo($"Sidebar: {Plans.Count} results, inventory={(inventory ? "yes" : "none")}");
            }
        }

        // The panel's rect is wider than the screen, so its left edge can be off screen. Once the layout has run, move the
        // sidebar right until it starts a margin inside the screen's left edge.
        private void ClampToScreen()
        {
            RectTransform rect = (RectTransform)root.transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 left = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 right = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            float pixelsPerUnit = rect.rect.width > 0f ? (right.x - left.x) / rect.rect.width : 0f;
            float marginPixels = Margin * pixelsPerUnit;
            if (pixelsPerUnit > 0f && left.x < marginPixels)
            {
                rect.anchoredPosition += new Vector2((marginPixels - left.x) / pixelsPerUnit, 0f);
            }
            if (RecipeHelperPlugin.Settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo($"Sidebar: screen x {left.x:0}-{right.x:0} of {Screen.width}, moved to {rect.anchoredPosition.x:0}");
            }
        }

        // Where the sidebar ended up, for when it doesn't show.
        private void LogLayout()
        {
            RectTransform panelRect = (RectTransform)transform;
            RectTransform rect = (RectTransform)root.transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            RecipeHelperPlugin.Log.LogInfo($"Sidebar layout: panel rect={panelRect.rect} scale={panelRect.lossyScale}; sidebar rect={rect.rect} pos={rect.position} active={root.activeInHierarchy}; canvas='{(canvas ? canvas.name : "none")}' order={(canvas ? canvas.sortingOrder : 0)} override={(canvas && canvas.overrideSorting)}; body chars={body.text.Length} font={(body.font ? body.font.name : "none")} size={body.fontSize}");
            foreach (Canvas own in GetComponentsInChildren<Canvas>(true))
            {
                RecipeHelperPlugin.Log.LogInfo($"  canvas under panel: '{own.name}' order={own.sortingOrder} override={own.overrideSorting}");
            }
            foreach (CanvasGroup group in GetComponentsInParent<CanvasGroup>(true))
            {
                RecipeHelperPlugin.Log.LogInfo($"  canvas group above: '{group.name}' alpha={group.alpha}");
            }
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
            // Inside the panel, not beside it: the panel draws on its own canvas above the HUD.
            Transform parent = transform;
            root.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(Margin, 0f);
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
