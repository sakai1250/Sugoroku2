using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>画面間で共有する余白、操作領域、閉じるボタンの配置規則。</summary>
    public static class UiSafeLayout
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float OuterMargin = 24f;
        public const float Gap = 12f;
        public const float HeaderHeight = 64f;
        public const float CloseButtonSize = 48f;
        public const float MinimumButtonHeight = 48f;
        public const float TopBarHeight = 92f;
        public const float LeftPanelWidth = 340f;
        public const float BottomBarHeight = 116f;

        public static Rect BoardViewportRect => new(
            LeftPanelWidth / ReferenceWidth,
            BottomBarHeight / ReferenceHeight,
            1f - LeftPanelWidth / ReferenceWidth,
            1f - (TopBarHeight + BottomBarHeight) / ReferenceHeight);

        public static void LayoutCloseButton(Transform panel, Transform closeButton)
        {
            if (panel == null || closeButton == null) return;

            closeButton.SetParent(panel, false);
            var rt = closeButton as RectTransform ?? closeButton.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = Vector2.one;
            rt.anchorMax = Vector2.one;
            rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);

            var layout = closeButton.GetComponent<LayoutElement>()
                ?? closeButton.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            closeButton.SetAsLastSibling();
        }

        public static void Stretch(RectTransform rt, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rt == null) return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void DisableDecorativeRaycasts(Transform root)
        {
            if (root == null) return;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<Selectable>() == null &&
                    image.GetComponentInParent<Selectable>() == null)
                    image.raycastTarget = false;
            }
        }
    }
}
