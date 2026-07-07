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
        public const float BodyFontSize    = 14f * HudTextStyle.TextScale;
        public const float TitleFontSize   = 26f * HudTextStyle.TextScale;
        public const float TagFontSize     = 12f * HudTextStyle.TextScale;
        public const float ChoiceFontSize  = 16f * HudTextStyle.TextScale;
        public const float PreviewFontSize = 12f * HudTextStyle.TextScale;
        public const float DisabledAlpha   = 0.4f;

        public static void Apply(Transform modalRoot, EventMaster ev)
        {
            if (modalRoot == null) return;

            var canvas = modalRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
                UiLayerManager.EnsureEventModalRoot(canvas);

            GameUiChrome.ApplySurface(modalRoot, new Color(0.12f, 0.14f, 0.20f, 0.98f));
            EnsureHeader(modalRoot, ev);
            ApplyDescriptionStyle(modalRoot);
            EnsureChoiceList(modalRoot);
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
                hrt.sizeDelta = new Vector2(720f, 44f);

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
                var img = go.AddComponent<Image>();
                img.color = EventTagColors.GetPanelColor(new[] { tag });
                GameUiChrome.ApplySurface(go.transform, EventTagColors.GetPanelColor(new[] { tag }), accent: false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 26f);

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"[{tag}]";
                tmp.fontSize = TagFontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                JapaneseFontProvider.Apply(tmp);
                var trt = textGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
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
            rt.sizeDelta = new Vector2(700f, 100f);
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
            rt.anchoredPosition = new Vector2(0f, -140f);
            rt.sizeDelta = new Vector2(700f, 320f);
            parent.SetAsLastSibling();
        }
    }
}
