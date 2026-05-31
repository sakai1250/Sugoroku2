using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Board;

namespace Sugoroku.UI
{
    /// <summary>ゲーム全体で使う、硬めで読みやすいUIクローム。</summary>
    public static class GameUiChrome
    {
        public static readonly Color Surface        = new(0.11f, 0.14f, 0.20f, 0.94f);
        public static readonly Color SurfaceSoft    = new(0.16f, 0.19f, 0.27f, 0.88f);
        public static readonly Color SurfaceRaised  = new(0.20f, 0.24f, 0.33f, 0.96f);
        public static readonly Color SurfacePressed = new(0.08f, 0.10f, 0.15f, 0.98f);
        public static readonly Color Stroke         = new(0.68f, 0.72f, 0.78f, 0.32f);
        public static readonly Color StrokeStrong   = new(0.88f, 0.90f, 0.86f, 0.48f);
        public static readonly Color Primary        = new(0.78f, 0.58f, 0.20f, 0.98f);
        public static readonly Color PrimaryPressed = new(0.58f, 0.40f, 0.12f, 0.98f);
        public static readonly Color MutedText      = new(0.78f, 0.82f, 0.88f, 1f);
        public static readonly Color PrimaryText    = new(0.12f, 0.10f, 0.06f, 1f);

        public static void ApplySurface(Transform target, Color? color = null, bool accent = true)
        {
            if (target == null) return;

            var img = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
            img.sprite = BoardVisualUtility.GetSquareSprite();
            img.type = Image.Type.Sliced;
            img.color = color ?? Surface;
            img.raycastTarget = false;

            ApplyOutline(target, Stroke, new Vector2(1f, -1f));
            if (accent)
                EnsureEdge(target, "SurfaceTopRule", Edge.Top, StrokeStrong, 2f);
            EnsureEdge(target, "SurfaceShadow", Edge.Bottom, new Color(0f, 0f, 0f, 0.20f), 2f);
        }

        public static void ApplyStatCell(Transform target, Color accentColor)
        {
            if (target == null) return;
            ApplySurface(target, new Color(0.17f, 0.20f, 0.28f, 0.82f));
            EnsureEdge(target, "StatAccent", Edge.Left, WithAlpha(accentColor, 0.78f), 3f);
        }

        public static void ApplyButton(Button button, bool primary)
        {
            if (button == null) return;

            var img = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            img.sprite = BoardVisualUtility.GetSquareSprite();
            img.type = Image.Type.Sliced;
            img.color = primary ? Primary : SurfaceRaised;
            img.raycastTarget = true;
            button.targetGraphic = img;

            ApplyOutline(button.transform, primary ? StrokeStrong : Stroke, new Vector2(1f, -1f));
            EnsureEdge(button.transform, "ButtonAccent", Edge.Bottom,
                primary ? new Color(0.96f, 0.78f, 0.34f, 0.90f) : new Color(0.72f, 0.76f, 0.82f, 0.44f),
                primary ? 3f : 2f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary
                ? new Color(1.08f, 1.04f, 0.92f, 1f)
                : new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = primary ? PrimaryPressed : SurfacePressed;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.44f, 0.48f, 0.58f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                JapaneseFontProvider.Apply(tmp);
                tmp.color = primary ? PrimaryText : Color.white;
                tmp.fontStyle |= FontStyles.Bold;
                HudTextStyle.ApplyOutlineSafe(tmp, primary ? 0f : 0.12f, new Color(0f, 0f, 0f, 0.70f));
            }
        }

        public static void ApplyChoiceButton(Button button, bool interactable)
        {
            if (button == null) return;
            ApplyButton(button, primary: false);
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = interactable
                    ? new Color(0.18f, 0.22f, 0.31f, 0.96f)
                    : new Color(0.12f, 0.13f, 0.16f, 0.70f);
            EnsureEdge(button.transform, "ChoiceAccent", Edge.Left,
                interactable ? new Color(0.84f, 0.68f, 0.30f, 0.92f) : new Color(0.52f, 0.54f, 0.58f, 0.55f),
                5f);
        }

        public static void ApplyLabelBand(Transform target, Color color)
        {
            EnsureEdge(target, "LabelBand", Edge.Bottom, color, 42f);
        }

        public static void ApplyAccentRail(Transform target, Color color, float width = 5f)
        {
            EnsureEdge(target, "AccentRail", Edge.Left, color, width);
        }

        public static void ApplyReadable(TextMeshProUGUI tmp, Color color, FontStyles style = FontStyles.Normal)
        {
            if (tmp == null) return;
            JapaneseFontProvider.Apply(tmp);
            tmp.color = color;
            tmp.fontStyle = style;
            HudTextStyle.ApplyOutlineSafe(tmp, 0.10f, new Color(0f, 0f, 0f, 0.70f));
        }

        private static void ApplyOutline(Transform target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>() ?? target.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Image EnsureEdge(Transform target, string name, Edge edge, Color color, float size)
        {
            var existing = target.Find(name);
            Image img;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(target, false);
                img = go.GetComponent<Image>();
                go.GetComponent<LayoutElement>().ignoreLayout = true;
            }
            else
            {
                img = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }

            img.sprite = BoardVisualUtility.GetSquareSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;

            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = edge switch
            {
                Edge.Left => new Vector2(0f, 0f),
                Edge.Right => new Vector2(1f, 0f),
                Edge.Bottom => new Vector2(0f, 0f),
                _ => new Vector2(0f, 1f)
            };
            rt.anchorMax = edge switch
            {
                Edge.Left => new Vector2(0f, 1f),
                Edge.Right => new Vector2(1f, 1f),
                Edge.Bottom => new Vector2(1f, 0f),
                _ => new Vector2(1f, 1f)
            };
            rt.pivot = edge switch
            {
                Edge.Left => new Vector2(0f, 0.5f),
                Edge.Right => new Vector2(1f, 0.5f),
                Edge.Bottom => new Vector2(0.5f, 0f),
                _ => new Vector2(0.5f, 1f)
            };
            rt.sizeDelta = edge is Edge.Left or Edge.Right ? new Vector2(size, 0f) : new Vector2(0f, size);
            rt.anchoredPosition = Vector2.zero;
            img.transform.SetAsFirstSibling();
            return img;
        }

        private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

        private enum Edge
        {
            Top,
            Bottom,
            Left,
            Right
        }
    }
}
