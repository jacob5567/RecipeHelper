using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecipeHelper
{
    // Builders for the sidebar's uGUI pieces: layout rows and stacks, text, and item tiles.
    internal static class Ui
    {
        public static readonly Color Muted = new Color32(0x8f, 0xa3, 0xc7, 0xff);
        public static readonly Color Body = new Color32(0xc3, 0xc7, 0xd3, 0xff);
        public static readonly Color CardBackground = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color TileBackground = new Color32(0x17, 0x1b, 0x24, 0xff);
        private const float FrameWidth = 2f;
        private const float IconInset = 3f;
        // Frames of pickups an earlier step makes are drawn this faint, so the chain reads at a glance.
        private const float MadeFrameAlpha = 0.4f;

        public static RectTransform Rect(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Row(Transform parent, string name, float spacing)
        {
            RectTransform rect = Rect(parent, name);
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static RectTransform Stack(Transform parent, string name, float spacing, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            RectTransform rect = Rect(parent, name);
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = alignment == TextAnchor.UpperLeft;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static void Pad(RectTransform rect, int padding)
        {
            rect.GetComponent<HorizontalOrVerticalLayoutGroup>().padding = new RectOffset(padding, padding, padding, padding);
        }

        public static void Background(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        public static void Size(RectTransform rect, float width, float height)
        {
            LayoutElement element = Element(rect);
            element.minWidth = element.preferredWidth = width;
            element.minHeight = element.preferredHeight = height;
            element.flexibleWidth = element.flexibleHeight = 0f;
        }


        public static void Flexible(RectTransform rect)
        {
            Element(rect).flexibleWidth = 1f;
        }

        private static LayoutElement Element(RectTransform rect)
        {
            LayoutElement element = rect.GetComponent<LayoutElement>();
            return element ? element : rect.gameObject.AddComponent<LayoutElement>();
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float fontSize, Color color)
        {
            RectTransform rect = Rect(parent, "Text");
            HGTextMeshProUGUI label = rect.gameObject.AddComponent<HGTextMeshProUGUI>();
            label.richText = true;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.fontSize = fontSize;
            label.color = color;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        // A fixed-width text cell, for the + and → between tiles.
        public static void Glyph(Transform parent, string glyph, float tileSize)
        {
            TextMeshProUGUI label = Text(parent, glyph, 16f, Muted);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            Size(label.rectTransform, 14f, tileSize);
        }

        // An item's icon in a frame of its tier colour.
        public static RectTransform Tile(Transform parent, PickupIndex pickup, float size, bool madeByEarlierStep = false)
        {
            PickupDef def = PickupCatalog.GetPickupDef(pickup);
            RectTransform frame = Rect(parent, "Tile");
            Size(frame, size, size);
            Image frameImage = frame.gameObject.AddComponent<Image>();
            Color tier = def != null ? def.baseColor : Muted;
            tier.a = madeByEarlierStep ? MadeFrameAlpha : 1f;
            frameImage.color = tier;
            frameImage.raycastTarget = false;

            RectTransform inner = Rect(frame, "Background");
            Fill(inner, FrameWidth);
            Image innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.color = TileBackground;
            innerImage.raycastTarget = false;

            if (def != null)
            {
                RectTransform icon = Rect(frame, "Icon");
                Fill(icon, IconInset);
                Image iconImage = icon.gameObject.AddComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.enabled = false;
                AsyncImage async = icon.gameObject.AddComponent<AsyncImage>();
                async.renderer = iconImage;
                async.SetAssetOrDirectReference(def);
                iconImage.enabled = true;
            }
            return frame;
        }

        public static string Name(PickupIndex pickup)
        {
            PickupDef def = PickupCatalog.GetPickupDef(pickup);
            return def != null ? Language.GetString(def.nameToken) : "?";
        }

        public static Color TierColor(PickupIndex pickup)
        {
            PickupDef def = PickupCatalog.GetPickupDef(pickup);
            return def != null ? def.baseColor : Muted;
        }

        public static void Fill(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
