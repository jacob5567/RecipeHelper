using System.Collections.Generic;
using RoR2;

namespace RecipeHelper
{
    // The shortest known way to end up with one result: the crafts to make, in order.
    internal sealed class CraftPlan
    {
        public PickupIndex Result;
        public List<CraftStep> Steps;
    }
}
