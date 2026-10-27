using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecipeHelper
{
    // One box of the sidebar: a header over a stack of blocks. The sidebar uses one on each side of the CHEF's window.
    internal sealed class SidebarColumn
    {
        private const int Padding = 12;
        private const float Spacing = 10f;
        private const float HeaderFontSize = 24f;
        private const float LabelFontSize = 13f;
        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.62f);

        public readonly GameObject Root;
        public readonly RectTransform Rect;
        private readonly TextMeshProUGUI header;
        private readonly List<GameObject> content = new List<GameObject>();

        public SidebarColumn(Transform parent, string name)
        {
            Rect = Ui.Stack(parent, name, Spacing);
            Root = Rect.gameObject;
            Rect.SetAsLastSibling();
            Ui.Pad(Rect, Padding);
            Ui.Background(Rect, BackgroundColor);
            Root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            header = Ui.Text(Rect, "", HeaderFontSize, Color.white);
        }

        public float InnerWidth => Rect.sizeDelta.x - 2f * Padding;

        public int Count => content.Count;

        // Places the column with its left edge at x and its vertical centre at y, in the parent's local units.
        public void Place(float x, float y, float width)
        {
            Rect.anchorMin = Rect.anchorMax = ((RectTransform)Rect.parent).pivot;
            Rect.pivot = new Vector2(0f, 0.5f);
            Rect.anchoredPosition = new Vector2(x, y);
            Rect.sizeDelta = new Vector2(width, Rect.sizeDelta.y);
        }

        public void Show(string title)
        {
            Clear();
            Root.SetActive(true);
            header.text = title;
        }

        public void Hide()
        {
            Clear();
            Root.SetActive(false);
        }

        public GameObject AddLabel(string text)
        {
            GameObject label = Ui.Text(Rect, text, LabelFontSize, Ui.Muted).gameObject;
            content.Add(label);
            return label;
        }

        public GameObject AddText(string text)
        {
            GameObject line = Ui.Text(Rect, text, LabelFontSize, Ui.Body).gameObject;
            content.Add(line);
            return line;
        }

        public GameObject Add(SidebarBlock block)
        {
            block.Build(Rect, InnerWidth);
            GameObject built = Rect.GetChild(Rect.childCount - 1).gameObject;
            content.Add(built);
            return built;
        }

        public void Remove(GameObject item)
        {
            if (!item)
            {
                return;
            }
            content.Remove(item);
            // Deactivated first: Destroy waits for the end of the frame, and the layout is measured before then.
            item.SetActive(false);
            Object.Destroy(item);
        }

        public float Height()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
            return LayoutUtility.GetPreferredHeight(Rect);
        }

        private void Clear()
        {
            foreach (GameObject item in content.ToArray())
            {
                Remove(item);
            }
        }
    }
}
