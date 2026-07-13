using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Board;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>カットイン演出（大きなイラスト＋タイトル）の共通プレゼンタ。</summary>
    public static class EventIntroPresenter
    {
        private const float IntroDuration = 1.35f;
        private const float InFraction    = 0.30f; // 全体尺のうち入場に使う割合
        private const float OutStart      = 0.72f; // 退場開始タイミング

        /// <summary>カットイン1枚分のリクエスト。</summary>
        private struct CutInRequest
        {
            public string     Badge;
            public string     Who;
            public string     Title;
            public Sprite     Hero;
            public CutInStyle Style;
            public float      Duration;
            public bool       Flash;   // 全画面フラッシュ（超レア用）
        }

        /// <summary>イベントマス到達時のカットイン。ev.IsRare のときは超レア専用の濃い演出。</summary>
        public static IEnumerator Play(EventMaster ev, PlayerData player)
        {
            if (ev == null) yield break;

            var category = EventMasuArt.ResolveCategory(SquareType.Event, ev.Tags);
            var req = new CutInRequest
            {
                Badge    = ev.IsRare ? "★★ 超レアイベント!! ★★" : "★ イベント発生!",
                Who      = player != null ? PlayerIdentity.FormatHudLabel(player) : "",
                Title    = ev.Title ?? "",
                Hero     = EventMasuArt.GetHeroSprite(category),
                Style    = ev.IsRare ? CutInStyle.Spin : CutInStyleMap.PickEvent(ev),
                Duration = ev.IsRare ? IntroDuration * 1.35f : IntroDuration,
                Flash    = ev.IsRare,
            };

            yield return PlayCard(req, player);
        }

        /// <summary>中間発表など、イラストなしのテキスト専用カットイン。</summary>
        public static IEnumerator PlayAnnouncement(string badge, string title, CutInStyle style, bool strong)
        {
            var req = new CutInRequest
            {
                Badge    = badge ?? "",
                Who      = "",
                Title    = title ?? "",
                Hero     = null,
                Style    = style,
                Duration = strong ? IntroDuration * 1.2f : IntroDuration,
                Flash    = strong,
            };

            yield return PlayCard(req, null);
        }

        private static IEnumerator PlayCard(CutInRequest req, PlayerData player)
        {
            var canvas = FindGameUiCanvas();
            if (canvas == null) yield break;

            var root = new GameObject("EventIntroOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();

            var rootRt = root.GetComponent<RectTransform>();
            Stretch(rootRt);

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0f));
            Stretch(backdrop.rectTransform);

            Image flash = null;
            if (req.Flash)
            {
                flash = CreateImage(root.transform, "Flash", new Color(1f, 1f, 1f, 0f));
                Stretch(flash.rectTransform);
            }

            var card = new GameObject("IntroCard", typeof(RectTransform));
            card.transform.SetParent(root.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(760f, 520f);
            cardRt.anchoredPosition = Vector2.zero;
            var cardGroup = card.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;

            CreateImage(card.transform, "CardBg", new Color(0.08f, 0.10f, 0.16f, 0f));
            GameUiChrome.ApplySurface(card.transform, new Color(0.08f, 0.10f, 0.16f, 0.96f));

            var badge = CreateTmp(card.transform, "Badge", req.Badge, HudTextStyle.JuiceStatusFontSize, TextAlignmentOptions.Center);
            var badgeRt = badge.rectTransform;
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -16f);
            badgeRt.sizeDelta = new Vector2(680f, 48f);
            HudTextStyle.ApplyReadable(badge, HudTextStyle.JuiceStatusFontSize, new Color(1f, 0.82f, 0.28f), true);
            HudTextStyle.ApplyOutlineSafe(badge, HudTextStyle.JuiceOutlineWidth, HudTextStyle.OutlineColor);

            var artFrame = new GameObject("ArtFrame", typeof(RectTransform));
            artFrame.transform.SetParent(card.transform, false);
            var artRt = artFrame.GetComponent<RectTransform>();
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);
            artRt.anchoredPosition = new Vector2(0f, 24f);
            artRt.sizeDelta = new Vector2(680f, 300f);
            artFrame.AddComponent<RectMask2D>();

            if (req.Hero != null)
            {
                var art = CreateImage(artFrame.transform, "HeroArt", Color.white);
                ConfigureFittedArt(art, req.Hero);
            }
            else
            {
                var placeholder = CreateImage(artFrame.transform, "Placeholder", new Color(0.16f, 0.20f, 0.28f, 0.9f));
                Stretch(placeholder.rectTransform);
            }

            var whoTmp = CreateTmp(card.transform, "Who", req.Who, 16f, TextAlignmentOptions.Center);
            var whoRt = whoTmp.rectTransform;
            whoRt.anchorMin = whoRt.anchorMax = new Vector2(0.5f, 0f);
            whoRt.pivot = new Vector2(0.5f, 0f);
            whoRt.anchoredPosition = new Vector2(0f, 52f);
            whoRt.sizeDelta = new Vector2(680f, 28f);
            HudTextStyle.ApplyReadable(whoTmp, HudTextStyle.Scale(16f), new Color(0.82f, 0.88f, 1f), false);

            var titleTmp = CreateTmp(card.transform, "Title", req.Title, 28f, TextAlignmentOptions.Center);
            var titleRt = titleTmp.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0f);
            titleRt.anchoredPosition = new Vector2(0f, 12f);
            titleRt.sizeDelta = new Vector2(680f, 40f);
            HudTextStyle.ApplyReadable(titleTmp, HudTextStyle.Scale(28f), Color.white, true);

            var (shakeAmp, shakeDur) = CutInStyleMap.Impact(req.Style);
            if (req.Flash) shakeAmp *= 1.6f;
            BoardCameraController.ShakeInstance(shakeAmp, shakeDur);
            PulseMasuAtPlayer(player);

            float duration = GameConfig.AnimationDuration(req.Duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float tIn = Mathf.Clamp01(t / InFraction);
                float tOut = t > OutStart ? Mathf.InverseLerp(OutStart, 1f, t) : 0f;
                var (offset, scale, rot) = CutInMotion.Evaluate(req.Style, tIn, tOut);

                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / (InFraction * 0.7f), 1f));
                float fadeOut = t > OutStart ? 1f - Mathf.InverseLerp(OutStart, 1f, t) : 1f;
                float alpha = fadeIn * fadeOut;

                cardRt.anchoredPosition = offset;
                cardRt.localScale = new Vector3(scale.x, scale.y, 1f);
                cardRt.localEulerAngles = new Vector3(0f, 0f, rot);
                cardGroup.alpha = alpha;
                backdrop.color = new Color(0f, 0f, 0f, 0.72f * Mathf.Min(alpha * 1.2f, 1f));
                if (flash != null)
                {
                    float flashA = Mathf.Max(0f, 0.85f - t * 4f); // 冒頭で白フラッシュ→素早く消える
                    flash.color = new Color(1f, 1f, 1f, flashA);
                }
                yield return null;
            }

            Object.Destroy(root);
        }

        private static void PulseMasuAtPlayer(PlayerData player)
        {
            if (player == null || BoardManager.Instance == null) return;
            var wp = BoardManager.Instance.GetWaypoint(player);
            var card = wp != null ? wp.GetComponentInChildren<MassTextCardView>(true) : null;
            card?.PlayEventHighlight();
        }

        private static Canvas FindGameUiCanvas()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.GetComponentInChildren<EventModalUI>(true) != null)
                    return canvas;
            }
            return Object.FindFirstObjectByType<Canvas>();
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

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, string text,
            float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void ConfigureFittedArt(Image art, Sprite sprite)
        {
            var rt = art.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            art.sprite = sprite;
            art.preserveAspect = false;
            art.type = Image.Type.Simple;

            var fitter = art.GetComponent<AspectRatioFitter>() ?? art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }
    }
}
