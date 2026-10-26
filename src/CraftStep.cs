using RoR2;

namespace RecipeHelper
{
    // One craft in a plan: the recipe, the pickup chosen for each slot, and what it makes.
    internal sealed class CraftStep
    {
        public CraftableCatalog.RecipeEntry Recipe;
        public PickupIndex[] Ingredients;
        public PickupIndex Result => Recipe.result;
        public int Amount => Recipe.amountToDrop;
    }
}
