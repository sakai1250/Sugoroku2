using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Board;
using Sugoroku.Visual;

namespace Sugoroku.UI
{
    /// <summary>
    /// Kenney UI Pack / Game Icons を Canvas 上の Button・Slider・HUD に適用する。
    /// </summary>
    public static class KenneyUiStyler
    {
        public static void StyleCanvas(Canvas canvas, bool includeInactive = true)
        {
            if (canvas == null) return;

            foreach (var btn in canvas.GetComponentsInChildren<Button>(includeInactive))
                StyleButton(btn, IsPrimaryButton(btn));

            foreach (var slider in canvas.GetComponentsInChildren<Slider>(includeInactive))
                StyleSlider(slider);

            StyleResourceBar(canvas.transform);
        }

        public static void StyleButton(Button button, bool isPrimary = true)
        {
            if (button == null) return;

            GameUiChrome.ApplyButton(button, isPrimary);

            if (button.GetComponent<Sugoroku.Audio.UiSoundPlayer>() == null)
                button.gameObject.AddComponent<Sugoroku.Audio.UiSoundPlayer>();
        }

        private static bool IsPrimaryButton(Button button)
        {
            if (button == null) return false;
            string n = button.name;
            return n.Contains("Start") ||
                   n.Contains("Roll") ||
                   n.Contains("Confirm") ||
                   n.Contains("Resume");
        }

        public static void StyleSlider(Slider slider)
        {
            if (slider == null) return;

            var square = BoardVisualUtility.GetSquareSprite();

            var bgImg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = square;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = new Color(0.09f, 0.11f, 0.16f, 0.92f);
            }

            if (slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = square;
                    fillImg.type = Image.Type.Sliced;
                    fillImg.color = new Color(0.56f, 0.82f, 0.56f);
                }
            }

            var handleImg = slider.handleRect != null
                ? slider.handleRect.GetComponent<Image>()
                : slider.targetGraphic as Image;
            if (handleImg != null)
            {
                handleImg.sprite = square;
                handleImg.type = Image.Type.Sliced;
                handleImg.color = new Color(0.92f, 0.84f, 0.60f, 1f);
            }
        }

        public static void StyleResourceBar(Transform canvasRoot)
        {
            ResourceHudVisuals.SetupTopResourceBar(canvasRoot);
        }

        public static void EnsureStatIconPublic(Transform bar, string name, KenneyAssets.ResourceIcon kind, Color tint) =>
            EnsureStatIcon(bar, name, kind, tint);

        /// <summary>シーン上の DiceIcon にスプライトを設定（RectTransform は変更しない）。</summary>
        public static Image EnsureDiceDisplay(Transform canvasRoot)
        {
            var diceText = FindDeep(canvasRoot, "DiceResult");
            if (diceText == null) return null;

            var iconTransform = FindDeep(canvasRoot, "DiceIcon");
            Image img;

            if (iconTransform == null)
            {
                var go = new GameObject("DiceIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(diceText, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                iconTransform.SetParent(diceText, false);
                img = iconTransform.GetComponent<Image>();
                if (img == null) img = iconTransform.gameObject.AddComponent<Image>();
            }

            var iconRt = img.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(4f, 0f);
            iconRt.sizeDelta = new Vector2(32f, 32f);
            iconRt.SetAsFirstSibling();
            img.raycastTarget = false;

            ApplyDiceIconSprite(img, 6);
            return img;
        }

        public static void ApplyDiceIconSprite(Image img, int face)
        {
            if (img == null) return;

            var sprite = KenneyAssets.GetDiceHudIcon(face);
            if (sprite == null) return;

            img.sprite         = sprite;
            img.preserveAspect = true;
            img.color          = Color.white;
            img.enabled        = true;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureStatIcon(Transform bar, string name, KenneyAssets.ResourceIcon kind, Color tint)
        {
            var t = bar.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(bar, false);
                t = go.transform;
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(28, 28);
                go.AddComponent<Image>();
                var le = go.AddComponent<LayoutElement>();
                le.minWidth = le.minHeight = 28f;
                le.preferredWidth = le.preferredHeight = 28f;
            }

            var img = t.GetComponent<Image>();
            var sp  = KenneyAssets.GetResourceIcon(kind);
            if (img != null && sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = true;
                img.color  = tint;
            }
        }
    }
}
