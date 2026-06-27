using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>イベントモーダル背面に、到達したマスカードの画面を薄く表示する。</summary>
    public static class EventModalMassBackdrop
    {
        public const string RootName = "EventMassBackdrop";

        private const float CardWidth  = 1840f;
        private const float CardHeight = CardWidth * MassTextCardPrefabFactory.CardAspectHeight
                                         / MassTextCardPrefabFactory.CardAspectWidth;
        private const float ArtAlpha   = 0.32f;
        private const float FrameAlpha = 0.22f;
        private const float TitleAlpha = 0.42f;

        public static void Apply(Transform modalRoot, EventMaster ev, PlayerData player)
        {
            if (modalRoot == null || ev == null) return;

            Clear(modalRoot);

            var wp = ResolveWaypoint(player);
            var squareType = wp != null ? wp.Type : SquareType.Event;
            var category = EventMasuArt.ResolveCategory(squareType, ev.Tags);
            var heroSprite = EventMasuArt.GetHeroSprite(category)
                ?? EventMasuArt.GetCardSprite(category);

            FocusCameraOnMass(wp, player);

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(modalRoot, false);
            var rootRt = root.GetComponent<RectTransform>();
            Stretch(rootRt);

            var backdrop = modalRoot.Find("ModalBackdrop");
            int sibling = backdrop != null ? backdrop.GetSiblingIndex() + 1 : 0;
            root.transform.SetSiblingIndex(sibling);

            var card = new GameObject("MassCardGhost", typeof(RectTransform));
            card.transform.SetParent(root.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(CardWidth, CardHeight);
            cardRt.anchoredPosition = new Vector2(0f, 36f);

            var border = CreateImage(card.transform, "GhostBorder",
                new Color(0.58f, 0.62f, 0.72f, FrameAlpha));
            var borderRt = border.rectTransform;
            borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(CardWidth - 4f, CardHeight - 4f);
            border.sprite = BoardVisualUtility.GetPixelCardSprite();
            border.type = Image.Type.Sliced;

            var panel = CreateImage(card.transform, "GhostPanel",
                new Color(0.20f, 0.24f, 0.32f, FrameAlpha * 0.9f));
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(CardWidth - 12f, CardHeight - 12f);
            panel.sprite = BoardVisualUtility.GetPixelCardSprite();
            panel.type = Image.Type.Sliced;

            var clipGo = new GameObject("GhostArtClip", typeof(RectTransform));
            clipGo.transform.SetParent(panel.transform, false);
            var clipRt = clipGo.GetComponent<RectTransform>();
            Stretch(clipRt, 12f, 34f, 36f, 12f);
            clipGo.AddComponent<RectMask2D>();

            if (heroSprite != null)
            {
                var art = CreateImage(clipGo.transform, "GhostArt", Color.white);
                Stretch(art.rectTransform);
                art.sprite = heroSprite;
                art.preserveAspect = false;
                art.color = new Color(1f, 1f, 1f, ArtAlpha);
                var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = heroSprite.rect.width / Mathf.Max(1f, heroSprite.rect.height);
            }

            var title = ResolveMassTitle(wp, ev);
            if (!string.IsNullOrEmpty(title))
            {
                var titleGo = new GameObject("GhostTitle", typeof(RectTransform));
                titleGo.transform.SetParent(panel.transform, false);
                var titleRt = titleGo.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0f);
                titleRt.anchorMax = new Vector2(1f, 0f);
                titleRt.pivot = new Vector2(0.5f, 0f);
                titleRt.offsetMin = new Vector2(18f, 10f);
                titleRt.offsetMax = new Vector2(-18f, 44f);

                var tmp = titleGo.AddComponent<TextMeshProUGUI>();
                tmp.text = title;
                tmp.fontSize = HudTextStyle.Scale(24f);
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.BottomLeft;
                tmp.color = new Color(1f, 1f, 1f, TitleAlpha);
                tmp.raycastTarget = false;
                JapaneseFontProvider.Apply(tmp);
                HudTextStyle.ApplyOutlineSafe(tmp, 0.12f, new Color(0f, 0f, 0f, 0.55f));
            }

            var tag = Waypoint.GetTypeShortLabel(squareType);
            if (!string.IsNullOrEmpty(tag))
            {
                var tagGo = new GameObject("GhostTag", typeof(RectTransform));
                tagGo.transform.SetParent(panel.transform, false);
                var tagRt = tagGo.GetComponent<RectTransform>();
                tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
                tagRt.pivot = new Vector2(0f, 1f);
                tagRt.anchoredPosition = new Vector2(16f, -10f);
                tagRt.sizeDelta = new Vector2(180f, 24f);

                var tmp = tagGo.AddComponent<TextMeshProUGUI>();
                tmp.text = tag;
                tmp.fontSize = HudTextStyle.Scale(14f);
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = new Color(0.92f, 0.94f, 1f, TitleAlpha);
                tmp.raycastTarget = false;
                JapaneseFontProvider.Apply(tmp);
            }
        }

        public static void Clear(Transform modalRoot)
        {
            if (modalRoot == null) return;
            var existing = modalRoot.Find(RootName);
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }

        private static Waypoint ResolveWaypoint(PlayerData player)
        {
            if (player == null || BoardManager.Instance == null) return null;
            return BoardManager.Instance.GetWaypoint(player.BoardPosition);
        }

        private static void FocusCameraOnMass(Waypoint wp, PlayerData player)
        {
            if (wp == null || BoardCameraController.Instance == null) return;
            BoardCameraController.Instance.ZoomForEvent(wp.transform.position);
            wp.GetComponentInChildren<MassTextCardView>(true)?.PlayEventHighlight();
        }

        private static string ResolveMassTitle(Waypoint wp, EventMaster ev)
        {
            if (wp != null && !string.IsNullOrEmpty(wp.DisplayName))
                return wp.DisplayName;
            if (ev != null && !string.IsNullOrEmpty(ev.Title))
                return ev.Title;
            return "";
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

        private static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
