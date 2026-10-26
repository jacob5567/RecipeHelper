using BepInEx;
using BepInEx.Logging;
using RoR2;
using RoR2.UI;

namespace RecipeHelper
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RiskOfOptionsCompat.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class RecipeHelperPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "sevenstack.RecipeHelper";
        public const string PluginName = "RecipeHelper";
        public const string PluginVersion = PluginVersionInfo.Version;

        internal static ManualLogSource Log;
        internal static PluginConfig Settings;

        private void Awake()
        {
            Log = Logger;
            Settings = new PluginConfig(Config);

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.Register(Settings);
            }

            HUD.onHudTargetChangedGlobal += OnHudTargetChanged;
        }

        private void OnDestroy()
        {
            HUD.onHudTargetChangedGlobal -= OnHudTargetChanged;
        }

        // Fires whenever any HUD changes the body it is showing, so each HUD gets exactly one watcher.
        private static void OnHudTargetChanged(HUD hud)
        {
            if (Settings.Enabled.Value && hud && !hud.GetComponent<CraftingPanelWatcher>())
            {
                hud.gameObject.AddComponent<CraftingPanelWatcher>();
            }
        }

        private void Update()
        {
            if (Settings.DebugLogging.Value && Settings.DumpKey.Value.IsDown())
            {
                Dump();
            }
        }

        // Logs the catalog size and what the planner makes of the local player's inventory.
        private static void Dump()
        {
            LocalUser user = LocalUserManager.GetFirstLocalUser();
            Inventory inventory = user?.cachedMaster ? user.cachedMaster.inventory : null;
            RecipePlanner planner = RecipeBook.Planner;
            InventorySnapshot snapshot = InventorySnapshot.FromInventory(inventory);
            Log.LogInfo($"Recipe dump: {CraftableCatalog.GetAllRecipes().Length} recipes, inventory: {snapshot}");
            foreach (CraftPlan plan in planner.Plan(snapshot, Settings.MaxChainSteps.Value, RecipePlannerLimits.StateBudget))
            {
                Log.LogInfo($"  {PickupCatalog.GetPickupDef(plan.Result)?.internalName} in {plan.Steps.Count}: {string.Join(" ; ", plan.Steps.ConvertAll(s => string.Join("+", System.Array.ConvertAll(s.Ingredients, i => PickupCatalog.GetPickupDef(i)?.internalName)) + "->" + PickupCatalog.GetPickupDef(s.Result)?.internalName))}");
            }
        }
    }
}
