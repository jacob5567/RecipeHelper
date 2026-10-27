using System.Collections.Generic;
using RoR2;

namespace RecipeHelper
{
    // One slot of a craft as the sidebar shows it: the pickup the plan uses, the other held pickups that would also fit,
    // and whether the plan's own earlier step makes it.
    internal sealed class SlotDetail
    {
        public PickupIndex Chosen;
        public List<PickupIndex> Alternates = new List<PickupIndex>();
        public bool MadeByEarlierStep;
    }

    // A plan's crafts with their slot details, found by replaying the plan from the starting inventory.
    internal sealed class StepDetail
    {
        public CraftStep Step;
        public SlotDetail[] Slots;

        public static List<StepDetail> Build(InventorySnapshot start, CraftPlan plan)
        {
            List<StepDetail> details = new List<StepDetail>();
            HashSet<PickupIndex> made = new HashSet<PickupIndex>();
            InventorySnapshot state = start;
            foreach (CraftStep step in plan.Steps)
            {
                StepDetail detail = new StepDetail { Step = step, Slots = new SlotDetail[step.Ingredients.Length] };
                for (int slot = 0; slot < step.Ingredients.Length; slot++)
                {
                    PickupIndex chosen = step.Ingredients[slot];
                    SlotDetail slotDetail = new SlotDetail { Chosen = chosen, MadeByEarlierStep = made.Contains(chosen) };
                    PickupIndex[] accepted = step.Recipe.possibleIngredients[slot].pickups;
                    if (accepted != null)
                    {
                        foreach (PickupIndex option in accepted)
                        {
                            if (option != chosen && state.Count(option) > UsedByOtherSlots(step, slot, option))
                            {
                                slotDetail.Alternates.Add(option);
                            }
                        }
                    }
                    detail.Slots[slot] = slotDetail;
                }
                details.Add(detail);
                made.Add(step.Result);
                state = state.Apply(step);
            }
            return details;
        }

        private static int UsedByOtherSlots(CraftStep step, int slot, PickupIndex pickup)
        {
            int used = 0;
            for (int i = 0; i < step.Ingredients.Length; i++)
            {
                if (i != slot && step.Ingredients[i] == pickup)
                {
                    used++;
                }
            }
            return used;
        }
    }
}
