using UnityEngine;

namespace RecipeHelper
{
    // One piece of sidebar content, placed whole into a column: a recipe card, a group's grid, or the detail panel.
    internal sealed class SidebarBlock
    {
        // Shown above the block, and again at the top of a column it continues into. Null for none.
        public string Group;
        // How many results it lists, for the "and N more" line.
        public int Results;
        // Kept even if earlier blocks have to make room for it.
        public bool Required;
        // Builds the block as the last child of the column's rect, given the column's inner width.
        public System.Action<RectTransform, float> Build;
    }
}
