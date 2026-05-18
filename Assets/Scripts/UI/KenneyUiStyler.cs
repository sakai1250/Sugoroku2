using UnityEngine;
using UnityEngine.UI;
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
                StyleButton(btn);

            foreach (var slider in canvas.GetComponentsInChildren<Slider>(includeInactive))
                StyleSlider(slider);

            StyleResourceBar(canvas.transform);
        }

        public static void StyleButton(Button button, bool isPrimary = true)
        {
            if (button == null) return;

            var img = button.GetComponent<Image>();
            if (img == null) return;

            var sprite = KenneyAssets.LoadSprite(isPrimary
                ? KenneyAssets.UiPack.ButtonDepth
                : KenneyAssets.UiPack.ButtonFlat);

            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }

            if (button.GetComponent<Sugoroku.Audio.UiSoundPlayer>() == null)
                button.gameObject.AddComponent<Sugoroku.Audio.UiSoundPlayer>();
        }

        public static void StyleSlider(Slider slider)
        {
            if (slider == null) return;

            var track = KenneyAssets.LoadSprite(KenneyAssets.UiPack.SliderTrack);
            var fill  = KenneyAssets.LoadSprite(KenneyAssets.UiPack.SliderFill);

            if (slider.fillRect != null && fill != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = fill;
                    fillImg.color = new Color(0.35f, 0.75f, 0.45f);
                }
            }

            if (slider.targetGraphic is Image bg && track != null)
            {
                bg.sprite = track;
                bg.color = new Color(0.35f, 0.35f, 0.4f);
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
                var diceRt = diceText as RectTransform;
                var parent = diceRt != null ? diceRt.parent : canvasRoot;

                var go = new GameObject("DiceIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);

                var rt = go.GetComponent<RectTransform>();
                if (diceRt != null)
                {
                    rt.anchorMin = diceRt.anchorMin;
                    rt.anchorMax = diceRt.anchorMax;
                    rt.pivot     = new Vector2(1f, 0.5f);
                    rt.anchoredPosition = diceRt.anchoredPosition + new Vector2(-110f, 0f);
                    rt.sizeDelta = new Vector2(72f, 72f);
                }
                else
                {
                    rt.sizeDelta = new Vector2(72f, 72f);
                }

                img = go.GetComponent<Image>();
            }
            else
            {
                img = iconTransform.GetComponent<Image>();
                if (img == null) img = iconTransform.gameObject.AddComponent<Image>();
            }

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
