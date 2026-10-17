using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §4.1 — イベントモーダルのレイアウト。</summary>
    public static class EventModalLayout
    {
        public static float BodyFontSize    => HudTextStyle.Scale(14f);
        public static float TitleFontSize   => HudTextStyle.Scale(26f);
        public static float TagFontSize     => HudTextStyle.Scale(12f);
        public static float ChoiceFontSize  => HudTextStyle.Scale(16f);
        public static float PreviewFontSize => HudTextStyle.Scale(12f);
        public const float DisabledAlpha   = 0.4f;

        public static void Apply(Transform modalRoot, EventMaster ev)
        {
            if (modalRoot == null) return;
            var authored = StaticUiAuthoring.Capture(modalRoot);
            try
            {
            var canvas = modalRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
                UiLayerManager.EnsureEventModalRoot(canvas);

            GameUiChrome.ApplySurface(modalRoot, new Color(0.12f, 0.14f, 0.20f, 0.98f));
            EnsureHeader(modalRoot, ev);
            ApplyDescriptionStyle(modalRoot);
            EnsureChoiceList(modalRoot);
            UiSafeLayout.DisableDecorativeRaycasts(modalRoot);
            }
            finally { authored.Restore(); }
        }

        private static void EnsureHeader(Transform root, EventMaster ev)
        {
            var header = root.Find("ModalHeader");
            if (header == null)
            {
                var headerGo = new GameObject("ModalHeader", typeof(RectTransform));
                headerGo.transform.SetParent(root, false);
                header = headerGo.transform;
                var hlg = headerGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 12f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                var le = headerGo.AddComponent<LayoutElement>();
                le.minHeight = 40f;
                var hrt = headerGo.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0.5f, 1f);
                hrt.anchorMax = new Vector2(0.5f, 1f);
                hrt.pivot = new Vector2(0.5f, 1f);
                hrt.anchoredPosition = new Vector2(0f, 200f);
                hrt.sizeDelta = new Vector2(656f, 44f);

                var title = root.Find("ModalTitle");
                var tags  = root.Find("ModalTags");
                if (title != null) title.SetParent(header, false);
                if (tags != null) tags.gameObject.SetActive(false);
            }

            var badgeArea = header.Find("TagsBadgeArea");
            if (badgeArea == null)
            {
                var areaGo = new GameObject("TagsBadgeArea", typeof(RectTransform));
                areaGo.transform.SetParent(header, false);
                badgeArea = areaGo.transform;
                var badges = areaGo.AddComponent<HorizontalLayoutGroup>();
                badges.spacing = 6f;
                badges.childAlignment = TextAnchor.MiddleRight;
                badges.childControlWidth = badges.childControlHeight = true;
                badges.childForceExpandWidth = false;
                var le = areaGo.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
            }

            RebuildTagBadges(badgeArea, ev?.Tags);

            var titleTmp = header.Find("ModalTitle")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                titleTmp.fontSize = TitleFontSize;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.alignment = TextAlignmentOptions.Left;
            }
        }

        private static void RebuildTagBadges(Transform badgeArea, string[] tags)
        {
            if (badgeArea == null) return;
            for (int i = badgeArea.childCount - 1; i >= 0; i--)
                Object.Destroy(badgeArea.GetChild(i).gameObject);

            if (tags == null || tags.Length == 0) return;

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var go = new GameObject($"Tag_{tag}", typeof(RectTransform));
                go.transform.SetParent(badgeArea, false);
                var panelColor = EventTagColors.GetPanelColor(new[] { tag });
                var img = go.AddComponent<Image>();
                img.color = panelColor;
                GameUiChrome.ApplySurface(go.transform, panelColor, accent: false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 26f);

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"[{tag}]";
                tmp.fontSize = TagFontSize;
                tmp.alignment = TextAlignmentOptions.Center;

                // WCAG 1.4.3: パネルと白/暗の各文字色のコントラスト比を計算し、大きい方を採用
                float l = 0.2126f * Linearize(panelColor.r) + 0.7152f * Linearize(panelColor.g) + 0.0722f * Linearize(panelColor.b);
                const float DarkTextLum = 0.011f; // (0.06,0.05,0.04) の輝度
                float whiteContrast = (1.05f) / (l + 0.05f);
                float darkContrast  = (l + 0.05f) / (DarkTextLum + 0.05f);
                bool darkText = darkContrast > whiteContrast;
                tmp.color = darkText ? new Color(0.06f, 0.05f, 0.04f, 1f) : Color.white;
                JapaneseFontProvider.Apply(tmp);
                // 常に黒アウトラインを添えて実効コントラストを補強（明るい緑系パネル対策）
                HudTextStyle.ApplyOutlineSafe(tmp, 0.12f, new Color(0f, 0f, 0f, 0.95f));
                var trt = textGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
        }

        /// <summary>sRGB 0〜1 を線形 luminance に変換（WCAG 1.4.3 のコントラスト計算用）。</summary>
        private static float Linearize(float v)
        {
            v = Mathf.Clamp01(v);
            return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        private static void ApplyDescriptionStyle(Transform root)
        {
            var desc = root.Find("ModalDescription")?.GetComponent<TextMeshProUGUI>();
            if (desc == null) return;
            desc.fontSize = BodyFontSize;
            desc.alignment = TextAlignmentOptions.TopLeft;
            desc.color = GameUiChrome.MutedText;
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.raycastTarget = false;
            var rt = desc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 112f);
            rt.anchoredPosition = new Vector2(0f, 120f);
        }

        private static void EnsureChoiceList(Transform root)
        {
            var parent = root.Find("ChoiceButtonParent");
            if (parent == null) return;

            var v = parent.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var rt = parent.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -150f);
            rt.sizeDelta = new Vector2(700f, 300f);
            parent.SetAsLastSibling();
        }
    }
}
