using RoR2;
using RoR2.UI;

namespace RecipeHelper
{
    // Puts a craft's ingredients in the CHEF's slots, as clicking them in the dialog would.
    //
    // The one exception to this mod's read-only rule: SubmitChoice and ClearSlot are what the dialog's own item and
    // slot buttons call, so a host without the mod handles them exactly like the player's clicks. Nothing else on the
    // CraftingController is called from here, and confirming the craft is always left to the player.
    internal static class SlotFiller
    {
        // Whether every slot is filled and the slots hold exactly this craft's ingredients, in any order.
        public static bool Holds(CraftingController controller, CraftStep step)
        {
            PickupIndex[] current = controller ? controller.ingredients : null;
            if (current == null || current.Length != step.Ingredients.Length)
            {
                return false;
            }
            bool[] matched = new bool[current.Length];
            foreach (PickupIndex ingredient in step.Ingredients)
            {
                int slot = System.Array.FindIndex(current, (p) => p == ingredient);
                while (slot >= 0 && matched[slot])
                {
                    slot = System.Array.FindIndex(current, slot + 1, (p) => p == ingredient);
                }
                if (slot < 0)
                {
                    return false;
                }
                matched[slot] = true;
            }
            return true;
        }

        // Empties the filled slots and picks the craft's ingredients in slot order. Returns false, changing nothing,
        // when the dialog doesn't offer one of them.
        public static bool Fill(CraftingController controller, PickupPickerPanel picker, CraftStep step)
        {
            if (!controller || !picker || controller.ingredients == null || controller.ingredients.Length != step.Ingredients.Length)
            {
                return false;
            }
            if (Holds(controller, step))
            {
                return true;
            }
            foreach (PickupIndex ingredient in step.Ingredients)
            {
                if (ButtonIndex(picker, ingredient) < 0)
                {
                    return false;
                }
            }

            PickupIndex[] current = controller.ingredients;
            for (int slot = 0; slot < current.Length; slot++)
            {
                if (current[slot] != PickupIndex.none)
                {
                    controller.ClearSlot(slot);
                }
            }
            // The host handles these in the order sent, so each pick sees the slots the earlier ones left. The index is
            // looked up again before each pick: on the host, each pick redraws the dialog, and a sorting mod may then
            // reorder its buttons.
            foreach (PickupIndex ingredient in step.Ingredients)
            {
                int index = ButtonIndex(picker, ingredient);
                if (index >= 0)
                {
                    controller.SubmitChoice(index);
                }
            }
            return true;
        }

        // The index the dialog's button for this pickup passes to SubmitChoice: its position in the options the dialog
        // shows. That is the controller's own order unless a mod such as LookingGlass sorts the buttons, in which case
        // that mod maps the index back to the controller's order inside SubmitChoice.
        private static int ButtonIndex(PickupPickerPanel picker, PickupIndex pickup)
        {
            PickupPickerController.Option[] options = picker.pickupOptions;
            if (options == null)
            {
                return -1;
            }
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].pickup.pickupIndex == pickup)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
