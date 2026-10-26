using RoR2;

namespace RecipeHelper
{
    // The recipe planner for the current run. The entitlement filter depends on the run's enabled expansions, so the
    // planner is rebuilt when the run changes.
    internal static class RecipeBook
    {
        private static Run builtFor;
        private static RecipePlanner planner;

        public static RecipePlanner Planner
        {
            get
            {
                if (planner == null || builtFor != Run.instance)
                {
                    CraftableCatalog.RecipeEntry[] recipes = CraftableCatalog.GetAllRecipes();
                    if (Run.instance)
                    {
                        CraftableCatalog.FilterByEntitlement(ref recipes);
                    }
                    planner = new RecipePlanner(recipes);
                    builtFor = Run.instance;
                }
                return planner;
            }
        }
    }
}
