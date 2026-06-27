using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Board;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.UI
{
    /// <summary>タイトル画面背景にイベントマスとキャラクターを散らばして飾る。</summary>
    [DisallowMultipleComponent]
    public class TitleSceneDecorations : MonoBehaviour
    {
        public const string RootName = "TitleScatterRoot";

        private const float MassAlpha = 0.88f;
        private const float CharAlpha = 0.94f;

        private readonly struct ScatterSpec
        {
            public readonly Vector2 Anchor;
            public readonly Vector2 Position;
            public readonly float Rotation;
            public readonly float Scale;

            public ScatterSpec(float ax, float ay, float x, float y, float rot, float scale)
            {
                Anchor = new Vector2(ax, ay);
                Position = new Vector2(x, y);
                Rotation = rot;
                Scale = scale;
            }
        }

        private static readonly ScatterSpec[] MassSpecs =
        {
            new(0f, 1f,  120f, -90f,  -8f, 0.92f),
            new(1f, 1f, -140f, -120f,  11f, 1.05f),
            new(0f, 0f,  150f,  110f,   7f, 0.88f),
            new(1f, 0f, -130f,  130f, -12f, 0.96f),
            new(0f, 0.5f,  80f,   40f,  -5f, 0.78f),
            new(1f, 0.5f, -90f,  -30f,   9f, 0.82f),
            new(0.5f, 1f, -260f, -70f,  -14f, 0.74f),
            new(0.5f, 0f,  280f,  60f,   6f, 0.80f),
        };

        private static readonly (CharacterType type, ScatterSpec spec)[] CharacterSpecs =
        {
            (CharacterType.Hobbyist, new(0f, 1f,  260f, -170f, -6f, 1.0f)),
            (CharacterType.Serious,  new(1f, 1f, -270f, -150f,  8f, 0.95f)),
            (CharacterType.Athletic, new(0f, 0f,  240f,  180f,  5f, 1.02f)),
            (CharacterType.Rich,     new(1f, 0f, -250f,  200f, -9f, 0.98f)),
            (CharacterType.Genius,   new(0.5f, 1f,  320f, -220f,  4f, 0.92f)),
        };

        private static readonly EventMasuArt.Category[] MassCategories =
        {
            EventMasuArt.Category.Event,
            EventMasuArt.Category.Research,
            EventMasuArt.Category.Lab,
            EventMasuArt.Category.Economy,
            EventMasuArt.Category.Event,
            EventMasuArt.Category.Research,
            EventMasuArt.Category.Lab,
            EventMasuArt.Category.Economy,
        };

        public static void Ensure(Transform canvas)
        {
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<TitleSceneDecorations>(true) != null) return;

            var go = new GameObject(nameof(TitleSceneDecorations), typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            go.AddComponent<TitleSceneDecorations>();
        }

        private void Awake()
        {
            EventMasuArt.Prewarm();
            Build();
        }

        private void Build()
        {
            var existing = transform.Find(RootName);
            if (existing != null)
                Destroy(existing.gameObject);

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            Stretch(rootRt);
            root.transform.SetAsFirstSibling();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                transform.SetAsFirstSibling();

            for (int i = 0; i < MassSpecs.Length; i++)
            {
                var sprite = EventMasuArt.GetCardSprite(MassCategories[i])
                    ?? EventMasuArt.GetHeroSprite(MassCategories[i]);
                if (sprite == null) continue;
                CreateMassCard(root.transform, $"ScatterMass_{i:D2}", MassSpecs[i], sprite);
            }

            foreach (var (type, spec) in CharacterSpecs)
            {
                var sprite = OriginalcharAssets.GetSprite(type);
                if (sprite == null) continue;
                CreateCharacter(root.transform, $"ScatterChar_{type}", spec, sprite, type.AccentColor());
            }

            var drift = root.AddComponent<TitleScatterDrift>();
            drift.Configure(root.transform);
        }

        private static void CreateMassCard(Transform parent, string name, ScatterSpec spec, Sprite sprite)
        {
            const float width = 700f;
            float height = width * MassTextCardPrefabFactory.CardAspectHeight
                             / MassTextCardPrefabFactory.CardAspectWidth;

            var card = new GameObject(name, typeof(RectTransform));
            card.transform.SetParent(parent, false);
            ApplyScatterRect(card.GetComponent<RectTransform>(), spec, new Vector2(width, height));

            var shadow = CreateImage(card.transform, "DepthShadow",
                new Color(0f, 0f, 0f, MassAlpha * 0.42f));
            SetCenterSize(shadow.rectTransform, new Vector2(width + 14f, height + 14f));
            shadow.rectTransform.anchoredPosition = new Vector2(8f, -10f);
            shadow.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            shadow.type = Image.Type.Simple;
            shadow.transform.SetAsFirstSibling();

            var border = CreateImage(card.transform, "Border",
                new Color(0.48f, 0.54f, 0.68f, MassAlpha));
            SetCenterSize(border.rectTransform, new Vector2(width - 4f, height - 4f));
            border.sprite = BoardVisualUtility.GetPixelCardSprite();
            border.type = Image.Type.Sliced;

            var panel = CreateImage(card.transform, "Panel",
                new Color(0.10f, 0.12f, 0.18f, MassAlpha * 0.96f));
            SetCenterSize(panel.rectTransform, new Vector2(width - 10f, height - 10f));
            panel.sprite = BoardVisualUtility.GetPixelCardSprite();
            panel.type = Image.Type.Sliced;

            var clipGo = new GameObject("ArtClip", typeof(RectTransform));
            clipGo.transform.SetParent(panel.transform, false);
            var clipRt = clipGo.GetComponent<RectTransform>();
            Stretch(clipRt, 8f, 8f, 8f, 8f);
            clipGo.AddComponent<RectMask2D>();

            var art = CreateImage(clipGo.transform, "Art", Color.white);
            Stretch(art.rectTransform);
            art.sprite = sprite;
            art.color = new Color(0.92f, 0.94f, 1f, MassAlpha);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }

        private static void CreateCharacter(Transform parent, string name, ScatterSpec spec,
            Sprite sprite, Color accent)
        {
            const float size = 300f;

            var card = new GameObject(name, typeof(RectTransform));
            card.transform.SetParent(parent, false);
            ApplyScatterRect(card.GetComponent<RectTransform>(), spec, new Vector2(size, size + 18f));

            var halo = CreateImage(card.transform, "Halo",
                new Color(accent.r, accent.g, accent.b, CharAlpha * 0.38f));
            SetCenterSize(halo.rectTransform, new Vector2(size + 24f, size + 30f));
            halo.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            halo.type = Image.Type.Simple;

            var frame = CreateImage(card.transform, "Frame",
                new Color(accent.r * 0.38f, accent.g * 0.38f, accent.b * 0.38f, CharAlpha));
            SetCenterSize(frame.rectTransform, new Vector2(size, size + 10f));
            frame.sprite = BoardVisualUtility.GetPixelCardSprite();
            frame.type = Image.Type.Sliced;

            var portraitShade = CreateImage(card.transform, "PortraitShade",
                new Color(0f, 0f, 0f, CharAlpha * 0.18f));
            SetCenterSize(portraitShade.rectTransform, new Vector2(size - 8f, size - 8f));
            portraitShade.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            portraitShade.type = Image.Type.Simple;

            var portrait = CreateImage(card.transform, "Portrait", Color.white);
            SetCenterSize(portrait.rectTransform, new Vector2(size - 16f, size - 16f));
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            portrait.color = new Color(1f, 1f, 1f, CharAlpha);
        }

        private static void ApplyScatterRect(RectTransform rt, ScatterSpec spec, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = spec.Anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size * spec.Scale;
            rt.anchoredPosition = spec.Position;
            rt.localRotation = Quaternion.Euler(0f, 0f, spec.Rotation);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetCenterSize(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rt)
        {
            Stretch(rt, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }

    /// <summary>散らした装飾にゆるい浮遊感を付ける。</summary>
    internal class TitleScatterDrift : MonoBehaviour
    {
        private Transform _root;
        private readonly System.Collections.Generic.List<DriftItem> _items = new();

        private struct DriftItem
        {
            public Transform Target;
            public Vector2 BasePosition;
            public float Phase;
            public float Speed;
            public float Amount;
        }

        public void Configure(Transform root)
        {
            _root = root;
            _items.Clear();
            if (_root == null) return;

            int index = 0;
            foreach (Transform child in _root)
            {
                var rt = child as RectTransform;
                if (rt == null) continue;
                _items.Add(new DriftItem
                {
                    Target = child,
                    BasePosition = rt.anchoredPosition,
                    Phase = index * 0.73f,
                    Speed = 0.55f + (index % 3) * 0.12f,
                    Amount = 4f + (index % 4) * 1.5f
                });
                index++;
            }
        }

        private void Update()
        {
            if (_items.Count == 0) return;
            float t = Time.unscaledTime;

            foreach (var item in _items)
            {
                if (item.Target == null) continue;
                var rt = item.Target as RectTransform;
                if (rt == null) continue;

                float bob = Mathf.Sin((t * item.Speed) + item.Phase) * item.Amount;
                float sway = Mathf.Cos((t * item.Speed * 0.7f) + item.Phase) * (item.Amount * 0.35f);
                rt.anchoredPosition = item.BasePosition + new Vector2(sway, bob);
            }
        }
    }
}
