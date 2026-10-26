using System.Collections.Generic;
using System.Linq;
using RoR2;

namespace RecipeHelper
{
    // Searches breadth-first over crafts for every result reachable from an inventory, keeping the shortest plan for each.
    // Only reads the recipe catalog; nothing here touches the CHEF or the inventory.
    internal sealed class RecipePlanner
    {
        private readonly CraftableCatalog.RecipeEntry[] recipes;
        private readonly HashSet<PickupIndex> intermediates = new HashSet<PickupIndex>();

        public bool HitBudget { get; private set; }

        public RecipePlanner(CraftableCatalog.RecipeEntry[] recipes)
        {
            this.recipes = recipes;
            foreach (CraftableCatalog.RecipeEntry recipe in recipes)
            {
                foreach (CraftableCatalog.IngredientSlotEntry slot in recipe.possibleIngredients)
                {
                    if (slot.pickups == null)
                    {
                        continue;
                    }
                    foreach (PickupIndex pickup in slot.pickups)
                    {
                        intermediates.Add(pickup);
                    }
                }
            }
        }

        private sealed class Node
        {
            public InventorySnapshot State;
            public List<CraftStep> Path;
        }

        public List<CraftPlan> Plan(InventorySnapshot start, int maxSteps, int stateBudget)
        {
            HitBudget = false;
            Dictionary<PickupIndex, CraftPlan> best = new Dictionary<PickupIndex, CraftPlan>();
            HashSet<string> seen = new HashSet<string> { start.Key() };
            List<Node> frontier = new List<Node> { new Node { State = start, Path = new List<CraftStep>() } };
            int visited = 0;

            for (int depth = 1; depth <= maxSteps && frontier.Count > 0; depth++)
            {
                List<Node> next = new List<Node>();
                foreach (Node node in frontier)
                {
                    foreach (CraftStep step in EnumerateCrafts(node.State))
                    {
                        List<CraftStep> path = new List<CraftStep>(node.Path) { step };
                        if (!best.ContainsKey(step.Result))
                        {
                            best[step.Result] = new CraftPlan { Result = step.Result, Steps = path };
                        }

                        if (depth == maxSteps || !intermediates.Contains(step.Result))
                        {
                            continue;
                        }
                        InventorySnapshot after = node.State.Apply(step);
                        if (!seen.Add(after.Key()))
                        {
                            continue;
                        }
                        if (++visited > stateBudget)
                        {
                            HitBudget = true;
                            return Sorted(best);
                        }
                        next.Add(new Node { State = after, Path = path });
                    }
                }
                frontier = next;
            }
            return Sorted(best);
        }

        private static List<CraftPlan> Sorted(Dictionary<PickupIndex, CraftPlan> best)
        {
            return best.Values.OrderBy(p => p.Steps.Count).ThenBy(p => p.Result.value).ToList();
        }

        // Every distinct way to fill a recipe's slots from distinct units of the inventory.
        internal IEnumerable<CraftStep> EnumerateCrafts(InventorySnapshot state)
        {
            foreach (CraftableCatalog.RecipeEntry recipe in recipes)
            {
                HashSet<string> emitted = new HashSet<string>();
                PickupIndex[] chosen = new PickupIndex[recipe.possibleIngredients.Length];
                foreach (PickupIndex[] fill in Fill(recipe, state, chosen, 0))
                {
                    string key = string.Join(",", fill.Select(p => p.value).OrderBy(v => v));
                    if (emitted.Add(key))
                    {
                        yield return new CraftStep { Recipe = recipe, Ingredients = (PickupIndex[])fill.Clone() };
                    }
                }
            }
        }

        private static IEnumerable<PickupIndex[]> Fill(CraftableCatalog.RecipeEntry recipe, InventorySnapshot state,
            PickupIndex[] chosen, int slot)
        {
            if (slot == chosen.Length)
            {
                yield return chosen;
                yield break;
            }

            PickupIndex[] options = recipe.possibleIngredients[slot].pickups;
            if (options == null)
            {
                yield break;
            }
            foreach (PickupIndex option in options)
            {
                int used = 0;
                for (int i = 0; i < slot; i++)
                {
                    if (chosen[i] == option)
                    {
                        used++;
                    }
                }
                if (state.Count(option) <= used)
                {
                    continue;
                }
                chosen[slot] = option;
                foreach (PickupIndex[] fill in Fill(recipe, state, chosen, slot + 1))
                {
                    yield return fill;
                }
            }
        }
    }
}
