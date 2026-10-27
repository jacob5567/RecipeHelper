using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RecipeHelper
{
    // Lives on the CHEF's CraftingPanel, so it goes away with the dialog. Lists what the participant's inventory can
    // craft, recomputed only when the inventory changes. The list fills the space left of the CHEF's window, then
    // continues in the space to its right. The Style setting picks recipe cards or a result grid with a detail panel.
    //
    // Read-only: never call the CraftingController's ClearSlot, ClearAllSlots, SendToSlot, ConfirmSelection,
    // ConfirmButtonHit, CancelButtonHit, OnConfirmServer or SendToMealprep, or set its ingredients. They change the
    // dialog's state or send messages to the host.
    internal sealed class RecipeSidebar : MonoBehaviour
    {
        private const string Header = "You can craft";
        private const string ContinuedHeader = "You can craft (continued)";
        private const string NothingHeader = "Nothing to craft";
        private const int SettleFrames = 3;

        private CraftingPanel panel;
        private Inventory inventory;
        private SidebarColumn left;
        private SidebarColumn right;
        private bool rendered;
        private bool lastTruncated;
        // The CHEF's window scales in when it opens, so the columns are placed only once it has held still.
        private bool settled;
        private int stillFrames;
        private Rect lastWindow;
        private InventorySnapshot snapshot;
        private PickupIndex selected = PickupIndex.none;

        // Read by the RecipeHelper.Tests plugin.
        internal List<CraftPlan> Plans { get; private set; } = new List<CraftPlan>();

        public void Init(HUD owner)
        {
            panel = GetComponent<CraftingPanel>();
            // Inside the panel, not beside it: the panel draws on its own canvas above the HUD.
            left = new SidebarColumn(transform, "RecipeHelperSidebarLeft");
            right = new SidebarColumn(transform, "RecipeHelperSidebarRight");
            right.Hide();
            RecipeHelperPlugin.Settings.Style.SettingChanged += OnStyleChanged;
            if (RecipeHelperPlugin.Settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo($"Sidebar: built on '{name}'");
            }
        }

        private void Update()
        {
            // The participant is synced after the panel appears, so the inventory is picked up here rather than in Init.
            CharacterMaster master = panel.craftingController?.networkUIPromptController?.currentParticipantMaster;
            Inventory current = master ? master.inventory : null;
            WatchWindow();
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

        private void WatchWindow()
        {
            Rect window = SidebarLayout.Anchor(panel, (RectTransform)transform);
            if (window != lastWindow)
            {
                lastWindow = window;
                stillFrames = 0;
                if (settled)
                {
                    settled = false;
                    left.Hide();
                    right.Hide();
                }
                return;
            }
            if (!settled && ++stillFrames >= SettleFrames)
            {
                settled = true;
                if (rendered)
                {
                    Render(lastTruncated);
                }
            }
        }

        private void OnStyleChanged(object sender, System.EventArgs args)
        {
            if (rendered)
            {
                Render(lastTruncated);
            }
        }

        private void OnDestroy()
        {
            RecipeHelperPlugin.Settings.Style.SettingChanged -= OnStyleChanged;
            if (inventory)
            {
                inventory.onInventoryChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            PluginConfig settings = RecipeHelperPlugin.Settings;
            RecipePlanner planner = RecipeBook.Planner;
            snapshot = settings.DebugAlwaysShow.Value ? EverythingSnapshot() : InventorySnapshot.FromInventory(inventory);
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
            lastTruncated = truncatedSearch;
            if (!settled)
            {
                return;
            }
            SidebarLayout layout = SidebarLayout.Measure((RectTransform)transform, panel);
            if (RecipeHelperPlugin.Settings.DebugLogging.Value)
            {
                RecipeHelperPlugin.Log.LogInfo($"Sidebar: {Plans.Count} results, inventory={(inventory ? "yes" : "none")}; {layout}; screen {Screen.width}x{Screen.height}; images:{SidebarLayout.DescribeImages((RectTransform)transform)}");
            }
            left.Place(layout.LeftX, layout.CenterY, layout.LeftWidth);
            right.Place(layout.RightX, layout.CenterY, layout.RightWidth);

            if (Plans.Count == 0)
            {
                left.Show(NothingHeader);
                right.Hide();
                return;
            }

            List<SidebarBlock> blocks = RecipeHelperPlugin.Settings.Style.Value == SidebarStyle.Grid
                ? GridView.Blocks(Plans, snapshot, selected, Select)
                : CardsView.Blocks(Plans, snapshot);
            Flow(blocks, layout.RightWidth > 0f ? new[] { left, right } : new[] { left }, layout.Height);
            if (layout.RightWidth <= 0f)
            {
                right.Hide();
            }
        }

        private void Select(PickupIndex result)
        {
            selected = result;
            Render(lastTruncated);
        }

        private sealed class Placed
        {
            public SidebarBlock Block;
            public GameObject Built;
            public GameObject Label;
        }

        // Fills the columns in order, moving to the next when a block doesn't fit. In the last column, a required block
        // pushes out earlier ones, and a line says how many results were left out.
        private void Flow(List<SidebarBlock> blocks, SidebarColumn[] columns, float maxHeight)
        {
            int column = 0;
            columns[0].Show(Header);
            for (int i = 1; i < columns.Length; i++)
            {
                columns[i].Hide();
            }
            List<Placed> placed = new List<Placed>();
            string lastGroup = null;
            int shownResults = 0;
            int totalResults = 0;
            foreach (SidebarBlock block in blocks)
            {
                totalResults += block.Results;
            }

            bool full = false;
            for (int i = 0; i < blocks.Count; i++)
            {
                SidebarBlock block = blocks[i];
                // Once the last column is full, only required blocks are still placed, so the order is never broken.
                if (full && !block.Required)
                {
                    continue;
                }
                SidebarColumn target = columns[column];
                Placed entry = new Placed { Block = block };
                if (block.Group != null && block.Group != lastGroup)
                {
                    entry.Label = target.AddLabel(block.Group);
                }
                entry.Built = target.Add(block);
                if (target.Height() <= maxHeight || target.Count <= 2)
                {
                    placed.Add(entry);
                    lastGroup = block.Group;
                    shownResults += block.Results;
                    continue;
                }

                target.Remove(entry.Built);
                target.Remove(entry.Label);
                if (column + 1 < columns.Length)
                {
                    column++;
                    columns[column].Show(ContinuedHeader);
                    placed.Clear();
                    lastGroup = null;
                    i--;
                    continue;
                }
                full = true;
                if (!block.Required)
                {
                    continue;
                }

                // Make room for the required block by dropping the blocks before it in this column, last first.
                float needed = EstimateHeight(target, block);
                for (int p = placed.Count - 1; p >= 0 && target.Height() > maxHeight - needed; p--)
                {
                    if (placed[p].Block.Required)
                    {
                        continue;
                    }
                    target.Remove(placed[p].Built);
                    target.Remove(placed[p].Label);
                    shownResults -= placed[p].Block.Results;
                    placed.RemoveAt(p);
                }
                target.Add(block);
            }

            if (shownResults < totalResults)
            {
                columns[column].AddText("and " + (totalResults - shownResults) + " more");
            }
            if (lastTruncated)
            {
                columns[column].AddText("Longer chains not searched.");
            }
        }

        // The height a block adds to a column, found by building it and taking it out again.
        private static float EstimateHeight(SidebarColumn column, SidebarBlock block)
        {
            float before = column.Height();
            GameObject built = column.Add(block);
            float after = column.Height();
            column.Remove(built);
            return after - before;
        }
    }

    internal static class RecipePlannerLimits
    {
        // Caps the search so a large inventory can't stall the dialog.
        public const int StateBudget = 20000;
    }
}
