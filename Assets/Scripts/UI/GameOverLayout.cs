using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>ゲームオーバー画面の左右分割レイアウト（左:演出 / 右:本文）。</summary>
    public static class GameOverLayout
    {
        public const float StageWidthFraction = 0.54f;
        const float TextPadLeft  = 28f;
        const float TextPadRight = 36f;

        public static void ApplyStage(RectTransform stage)
        {
            if (stage == null) return;

            stage.anchorMin = Vector2.zero;
            stage.anchorMax = new Vector2(StageWidthFraction, 1f);
            stage.offsetMin = stage.offsetMax = Vector2.zero;

            if (stage.GetComponent<RectMask2D>() == null)
                stage.gameObject.AddComponent<RectMask2D>();
        }

        public static void ApplyContent(Transform root, GameOverVisualStyle style)
        {
            if (root == null) return;

            EnsureTextBackdrop(root);
            var backdrop = root.Find("GameOverTextBackdrop");
            LayoutTitle(root.Find("GameOverTitle"));
            LayoutBody(root.Find("GameOverBody"), style);
            LayoutAccentPanel(root.Find("AccentPanel"), backdrop, style);
            LayoutButton(root.Find("TitleButton"));
        }

        static void EnsureTextBackdrop(Transform root)
        {
            var child = root.Find("GameOverTextBackdrop");
            Image img;
            RectTransform rt;

            if (child == null)
            {
                var go = new GameObject("GameOverTextBackdrop", typeof(RectTransform), typeof(Image));
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
            img.color = new Color(0.07f, 0.07f, 0.09f, 0.92f);
            img.raycastTarget = false;

            rt.anchorMin = new Vector2(StageWidthFraction, 0f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void LayoutTitle(Transform title)
        {
            if (title is not RectTransform rt) return;

            rt.anchorMin = new Vector2(StageWidthFraction, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(TextPadLeft, 170f);
            rt.offsetMax = new Vector2(-TextPadRight, 250f);

            var tmp = title.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopRight;
                ApplyColumnText(tmp, 28f, 16f);
            }
        }

        static void LayoutBody(Transform body, GameOverVisualStyle style)
        {
            if (body is not RectTransform rt) return;

            ApplyBodyRect(rt, style);

            var tmp = body.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopRight;
                tmp.color = new Color(0.82f, 0.82f, 0.88f);
                ApplyColumnText(tmp, 20f, 13f);
            }
        }

        static void ApplyColumnText(TextMeshProUGUI tmp, float maxSize, float minSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = maxSize;
            tmp.fontSizeMin = minSize;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        static void LayoutAccentPanel(Transform accent, Transform backdrop, GameOverVisualStyle style)
        {
            if (accent is not RectTransform rt) return;

            var (y, height) = GetBodyLayout(style);
            if (backdrop is RectTransform backdropRt)
            {
                rt.anchorMin = new Vector2(backdropRt.anchorMin.x, 0.5f);
                rt.anchorMax = new Vector2(backdropRt.anchorMax.x, 0.5f);
            }
            else
            {
                rt.anchorMin = new Vector2(StageWidthFraction, 0.5f);
                rt.anchorMax = Vector2.one;
            }

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, height);

            var img = accent.GetComponent<Image>();
            if (img == null) return;
            img.raycastTarget = false;
            if (img.sprite == null)
            {
                img.sprite = BoardVisualUtility.GetPixelCardSprite();
                img.type = Image.Type.Sliced;
            }
        }

        static (float y, float height) GetBodyLayout(GameOverVisualStyle style) =>
            style == GameOverVisualStyle.ExpulsionList
                ? (-30f, 220f)
                : (10f, 360f);

        static void ApplyBodyRect(RectTransform rt, GameOverVisualStyle style)
        {
            var (y, height) = GetBodyLayout(style);
            rt.anchorMin = new Vector2(StageWidthFraction, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(TextPadLeft, y - height * 0.5f);
            rt.offsetMax = new Vector2(-TextPadRight, y + height * 0.5f);
        }

        static void LayoutButton(Transform button)
        {
            if (button is not RectTransform rt) return;

            rt.anchorMin = new Vector2(StageWidthFraction, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(TextPadLeft, -298f);
            rt.offsetMax = new Vector2(-TextPadRight - 140f, -243f);
        }
    }
}
