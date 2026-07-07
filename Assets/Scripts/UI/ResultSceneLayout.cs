using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;

namespace Sugoroku.UI
{
    /// <summary>修了リザルト画面のテキスト列レイアウト（左:本文 / 右:進路カード）。</summary>
    public static class ResultSceneLayout
    {
        public const float TextColumnFraction = 0.58f;
        const float PadLeft  = 28f;
        const float PadRight = 24f;
        const float PadTop   = 88f;
        const float PadBottom = 120f;

        public static void Apply(Transform root)
        {
            if (root == null) return;

            EnsureTextBackdrop(root);
            LayoutTitle(root.Find("ResultTitle"));
            LayoutBody(root.Find("ResultBody"));
            LayoutTitleButton(root.Find("TitleButton"));
            LayoutCareerCard(root.Find("CareerDecision3DCard"));
            BringReadableContentToFront(root);
        }

        static void EnsureTextBackdrop(Transform root)
        {
            var child = root.Find("ResultTextBackdrop");
            Image img;
            RectTransform rt;

            if (child == null)
            {
                var go = new GameObject("ResultTextBackdrop", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                img = go.GetComponent<Image>();
                rt = go.GetComponent<RectTransform>();
            }
            else
            {
                img = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
                rt = child as RectTransform;
            }

            img.sprite = BoardVisualUtility.GetPixelCardSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
            img.raycastTarget = false;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(TextColumnFraction, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void LayoutTitle(Transform title)
        {
            if (title is not RectTransform rt) return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(TextColumnFraction, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(PadLeft, -PadTop);
            rt.offsetMax = new Vector2(-PadRight, -16f);

            ApplyTitleText(title.GetComponent<TextMeshProUGUI>());
        }

        static void LayoutBody(Transform body)
        {
            if (body is not RectTransform rt) return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(TextColumnFraction, 1f);
            rt.offsetMin = new Vector2(PadLeft, PadBottom);
            rt.offsetMax = new Vector2(-PadRight, -PadTop);

            ApplyBodyText(body.GetComponent<TextMeshProUGUI>());
        }

        static void LayoutTitleButton(Transform button)
        {
            if (button is not RectTransform rt) return;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 28f);
            rt.sizeDelta = new Vector2(280f, 52f);
        }

        static void LayoutCareerCard(Transform card)
        {
            if (card is not RectTransform rt) return;

            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-36f, 24f);
            rt.sizeDelta = new Vector2(350f, 142f);
        }

        static void ApplyTitleText(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(1f, 0.94f, 0.70f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 34f;
            tmp.fontSizeMin = 22f;
            HudTextStyle.ApplyOutlineSafe(tmp, 0.18f, new Color(0f, 0f, 0f, 0.90f));
        }

        static void ApplyBodyText(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(0.90f, 0.93f, 0.98f);
            tmp.lineSpacing = 4f;
            tmp.paragraphSpacing = 6f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 19f;
            tmp.fontSizeMin = 14f;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            HudTextStyle.ApplyOutlineSafe(tmp, 0.12f, new Color(0f, 0f, 0f, 0.85f));
        }

        public static void BringReadableContentToFront(Transform root)
        {
            if (root == null) return;

            var stage    = root.Find("EndSceneDepthBackdrop");
            var backdrop = root.Find("ResultTextBackdrop");
            var title    = root.Find("ResultTitle");
            var body     = root.Find("ResultBody");
            var card     = root.Find("CareerDecision3DCard");
            var button   = root.Find("TitleButton");
            var share    = root.Find("ShareButtonRow");

            int index = 12;
            if (stage != null) stage.SetSiblingIndex(index++);
            if (backdrop != null) backdrop.SetSiblingIndex(index++);
            if (card != null) card.SetSiblingIndex(index++);
            if (title != null) title.SetSiblingIndex(index++);
            if (body != null) body.SetSiblingIndex(index++);
            if (button != null) button.SetSiblingIndex(index++);
            if (share != null) share.SetSiblingIndex(index++);
        }
    }
}
