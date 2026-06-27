using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.Board
{
    /// <summary>Mass_TextCard 内の event-MASU イラスト配置。</summary>
    public static class MassTextCardArtLayout
    {
        public const string ClipName = "MassArtClip";
        public const string ImageName = "MassArtImage";

        // CardPanel 228x168 (4:3): 左アクセント 11px、右マーカー 36px、下タイトル帯 32px
        private static readonly Vector2 ClipOffsetMin = new(11f, 3f);
        private static readonly Vector2 ClipOffsetMax = new(-36f, -32f);

        public static void EnsurePanelMask(Transform panel)
        {
            if (panel == null) return;
            var mask = panel.GetComponent<RectMask2D>();
            if (mask != null)
                Object.Destroy(mask);
        }

        public static Image EnsureMassArtImage(Transform panel)
        {
            if (panel == null) return null;

            EnsurePanelMask(panel);
            RemoveDuplicateArt(panel);

            var clip = panel.Find(ClipName);
            if (clip == null)
            {
                var clipGo = new GameObject(ClipName, typeof(RectTransform));
                clipGo.transform.SetParent(panel, false);
                clipGo.transform.SetAsFirstSibling();
                clip = clipGo.transform;
                ApplyClipRect(clip);
                clipGo.AddComponent<RectMask2D>();
            }
            else
            {
                ApplyClipRect(clip);
            }

            var imgTr = clip.Find(ImageName);
            Image img;
            if (imgTr == null)
            {
                var imgGo = new GameObject(ImageName, typeof(RectTransform));
                imgGo.transform.SetParent(clip, false);
                img = imgGo.AddComponent<Image>();
                img.raycastTarget = false;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
                ConfigureCoverImage(img, null);
            }
            else
            {
                img = imgTr.GetComponent<Image>();
                ConfigureCoverImage(img, img != null ? img.sprite : null);
            }

            if (img != null && img.sprite == null)
                img.enabled = false;

            return img;
        }

        public static void ApplySprite(Image image, Sprite sprite)
        {
            if (image == null) return;

            if (sprite == null)
            {
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.color = Color.white;
            ConfigureCoverImage(image, sprite);
        }

        private static void ConfigureCoverImage(Image image, Sprite sprite)
        {
            if (image == null) return;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            var fitter = image.GetComponent<AspectRatioFitter>() ?? image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null && sprite.rect.height > 0f
                ? sprite.rect.width / sprite.rect.height
                : 4f / 3f;
        }

        private static void ApplyClipRect(Transform clip)
        {
            var clipRt = clip.GetComponent<RectTransform>();
            clipRt.anchorMin = Vector2.zero;
            clipRt.anchorMax = Vector2.one;
            clipRt.offsetMin = ClipOffsetMin;
            clipRt.offsetMax = ClipOffsetMax;
        }

        private static void RemoveDuplicateArt(Transform panel)
        {
            var clip = panel.Find(ClipName);
            var legacy = panel.Find(ImageName);
            if (clip == null && legacy != null)
            {
                Object.Destroy(legacy.gameObject);
                return;
            }

            if (clip != null && legacy != null)
                Object.Destroy(legacy.gameObject);

            if (clip == null) return;

            for (int i = clip.childCount - 1; i >= 0; i--)
            {
                var child = clip.GetChild(i);
                if (child.name == ImageName) continue;
                Object.Destroy(child.gameObject);
            }
        }
    }
}
