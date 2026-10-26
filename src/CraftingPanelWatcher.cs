using RoR2.UI;
using UnityEngine;

namespace RecipeHelper
{
    // One per HUD. The CHEF's dialog is instantiated under the HUD's main container, so a child count change is the cue
    // to look for a CraftingPanel that has no sidebar yet.
    internal sealed class CraftingPanelWatcher : MonoBehaviour
    {
        private HUD hud;
        private int lastChildCount;

        private void Awake()
        {
            hud = GetComponent<HUD>();
        }

        private void Update()
        {
            Transform container = hud ? hud.mainContainer.transform : null;
            if (!container || container.childCount == lastChildCount)
            {
                return;
            }
            lastChildCount = container.childCount;

            foreach (CraftingPanel panel in container.GetComponentsInChildren<CraftingPanel>())
            {
                if (!panel.GetComponent<RecipeSidebar>())
                {
                    panel.gameObject.AddComponent<RecipeSidebar>().Init(hud);
                }
            }
        }
    }
}
