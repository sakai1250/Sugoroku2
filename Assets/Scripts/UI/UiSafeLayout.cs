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

        /// <summary>
        /// 16:9より狭い(4:3・16:10)画面で左右に必要な余白(リファレンスpx・片側)。
        /// CanvasScaler.matchWidthOrHeight=0.5 前提。16:9/21:9では0を返し既存レイアウトに無影響。
        /// </summary>
        public static float HorizontalInset(float match = 0.5f)
        {
            int wpx = Screen.width, hpx = Screen.height;
            if (wpx <= 0 || hpx <= 0) return 0f;
            float scale = match * (wpx / ReferenceWidth) + (1f - match) * (hpx / ReferenceHeight);
            if (scale <= 0f) return 0f;
            float logicalW = wpx / scale;
            return Mathf.Max(0f, (ReferenceWidth - logicalW) * 0.5f);
        }

        /// <summary>16:9より広い(21:9)画面で上下に必要な余白(リファレンスpx・片側)。</summary>
        public static float VerticalInset(float match = 0.5f)
        {
            int wpx = Screen.width, hpx = Screen.height;
            if (wpx <= 0 || hpx <= 0) return 0f;
            float scale = match * (wpx / ReferenceWidth) + (1f - match) * (hpx / ReferenceHeight);
            if (scale <= 0f) return 0f;
            float logicalH = hpx / scale;
            return Mathf.Max(0f, (ReferenceHeight - logicalH) * 0.5f);
        }

        /// <summary>ScrollRect 用の標準スクロールバーを生成し割り当てる。右端・細幅・控えめな配色。</summary>
        public static Scrollbar CreateScrollbar(ScrollRect scroll, RectTransform viewport, Color handleColor)
        {
            if (scroll == null || viewport == null) return null;

            var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGo.transform.SetParent(viewport, false);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(1f, 0.5f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(8f, 0f);
            var barImg = barGo.GetComponent<Image>();
            barImg.color = new Color(0f, 0f, 0f, 0.25f);
            barImg.raycastTarget = false;

            var slidingGo = new GameObject("Sliding Area", typeof(RectTransform));
            slidingGo.transform.SetParent(barGo.transform, false);
            var slidingRt = slidingGo.GetComponent<RectTransform>();
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = slidingRt.offsetMax = new Vector2(1f, 1f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slidingRt, false);
            var handleImg = handleGo.GetComponent<Image>();
            handleImg.color = handleColor;
            handleImg.raycastTarget = true;
            var handleRt = handleImg.rectTransform;
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.offsetMin = handleRt.offsetMax = Vector2.zero;

            var bar = barGo.GetComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handleRt;
            bar.targetGraphic = handleImg;
            bar.size = 0.3f;

            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return bar;
        }
    }
}
