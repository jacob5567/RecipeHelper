using System.Text;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RecipeHelper
{
    // Where the sidebar's columns go, in the CHEF panel's local units: the gaps between the screen's edges and the
    // CHEF's window. The panel's rect is wider than the screen, so the screen's edges are found by projection rather
    // than taken from the rect.
    internal struct SidebarLayout
    {
        private const float Margin = 12f;
        private const float MinWidth = 160f;
        // Used when the CHEF's window can't be found or leaves too little room beside it.
        private const float FallbackWidth = 360f;

        public float LeftX;
        public float LeftWidth;
        public float RightX;
        public float RightWidth;
        public float CenterY;
        public float Height;
        public string WindowName;
        public string Candidates;

        public static SidebarLayout Measure(RectTransform panelRect, CraftingPanel panel)
        {
            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            Camera camera = canvas && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            Vector2 screenMin = ToLocal(panelRect, camera, Vector2.zero);
            Vector2 screenMax = ToLocal(panelRect, camera, new Vector2(Screen.width, Screen.height));

            SidebarLayout layout = new SidebarLayout
            {
                CenterY = (screenMin.y + screenMax.y) / 2f,
                Height = screenMax.y - screenMin.y - 2f * Margin,
            };

            StringBuilder trace = RecipeHelperPlugin.Settings.DebugLogging.Value ? new StringBuilder() : null;
            RectTransform window = FindWindow(panelRect, panel, camera, trace);
            layout.Candidates = trace?.ToString();
            if (window)
            {
                Vector3[] corners = new Vector3[4];
                window.GetWorldCorners(corners);
                Vector2 windowMin = ToLocal(panelRect, camera, RectTransformUtility.WorldToScreenPoint(camera, corners[0]));
                Vector2 windowMax = ToLocal(panelRect, camera, RectTransformUtility.WorldToScreenPoint(camera, corners[2]));
                layout.WindowName = window.name;
                layout.LeftX = screenMin.x + Margin;
                // The same margin from the screen's edges and from the window's sides.
                layout.LeftWidth = windowMin.x - Margin - layout.LeftX;
                layout.RightX = windowMax.x + Margin;
                layout.RightWidth = screenMax.x - Margin - layout.RightX;
            }
            if (layout.LeftWidth < MinWidth)
            {
                layout.LeftX = screenMin.x + Margin;
                layout.LeftWidth = FallbackWidth;
                layout.RightWidth = 0f;
            }
            if (layout.RightWidth < MinWidth)
            {
                layout.RightWidth = 0f;
            }
            return layout;
        }

        // Where the CHEF's ingredient slots are on screen. It changes while the window scales in as the dialog opens.
        public static Rect Anchor(CraftingPanel panel, RectTransform panelRect)
        {
            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            Camera camera = canvas && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            return ScreenRect(panel.ingredientContainer ? (RectTransform)panel.ingredientContainer : panelRect, camera);
        }

        // The CHEF's visible window: the largest image narrower than the screen that encloses both the ingredient
        // slots and the inventory grid. The panel itself spans more than the screen, and the images inside the frame
        // ('Background') are narrower than what is drawn.
        private static RectTransform FindWindow(RectTransform panelRect, CraftingPanel panel, Camera camera, StringBuilder trace)
        {
            if (!panel.ingredientContainer || !panel.inventoryContainer)
            {
                return null;
            }
            Rect slots = ScreenRect(panel.ingredientContainer, camera);
            Rect inventory = ScreenRect(panel.inventoryContainer, camera);

            RectTransform best = null;
            float bestArea = 0f;
            foreach (Image image in panelRect.GetComponentsInChildren<Image>())
            {
                if (!image.enabled || image.color.a <= 0f)
                {
                    continue;
                }
                RectTransform rect = image.rectTransform;
                Rect bounds = ScreenRect(rect, camera);
                // The inventory grid scrolls, so it can be taller than the frame; only its width has to fit.
                if (bounds.width >= Screen.width || !Encloses(bounds, slots) || bounds.xMin > inventory.xMin + 1f || bounds.xMax < inventory.xMax - 1f)
                {
                    continue;
                }
                trace?.Append($" {rect.name}={bounds.width:0}px");
                float area = bounds.width * bounds.height;
                if (area > bestArea)
                {
                    best = rect;
                    bestArea = area;
                }
            }
            return best;
        }


        private static bool Encloses(Rect outer, Rect inner)
        {
            return outer.xMin <= inner.xMin + 1f && outer.xMax >= inner.xMax - 1f
                && outer.yMin <= inner.yMin + 1f && outer.yMax >= inner.yMax - 1f;
        }

        private static Rect ScreenRect(RectTransform rect, Camera camera)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 ToLocal(RectTransform rect, Camera camera, Vector2 screenPoint)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, camera, out local);
            return local;
        }

        public override string ToString()
        {
            return $"window='{WindowName ?? "not found"}' left x={LeftX:0} w={LeftWidth:0}, right x={RightX:0} w={RightWidth:0}, height={Height:0}; candidates:{Candidates}";
        }
    }
}
