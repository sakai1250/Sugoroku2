using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>修了／ゲームオーバー画面のビジュアル差分（screen.md §5.1 / §5.2）。</summary>
    public static class EndSceneVisuals
    {
        public static void ApplyGraduation(Transform root, GraduationOutcome? heroOutcome)
        {
            if (root == null) return;

            var panel = root.GetComponent<Image>();
            if (panel != null)
            {
                panel.sprite = BoardVisualUtility.GetPixelCardSprite();
                panel.type = Image.Type.Sliced;
                panel.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
                panel.raycastTarget = false;
            }

            Color accentColor = heroOutcome?.AccentColor ?? new Color(0.86f, 0.68f, 0.28f, 1f);
            EnsureEndBackdrop(root, accentColor, danger: false);
            EnsureDepthFrame(root, accentColor, danger: false);
            EnsureCareerCard(root, heroOutcome);
            EnsureAnimator(root, danger: false);

            if (heroOutcome == null) return;

            var accent = root.Find("AccentBanner")?.GetComponent<Image>();
            if (accent == null)
            {
                var go = new GameObject("AccentBanner", typeof(RectTransform));
                go.transform.SetParent(root, false);
                go.transform.SetAsFirstSibling();
                accent = go.AddComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 8f);
                rt.anchoredPosition = Vector2.zero;
            }

            accent.sprite = BoardVisualUtility.GetPixelSolidSprite();
            accent.type = Image.Type.Sliced;
            accent.raycastTarget = false;
            accent.color = heroOutcome.Value.AccentColor;

            var title = root.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.color = heroOutcome.Value.AccentColor;
                HudTextStyle.ApplyOutlineSafe(title, 0.16f, new Color(0f, 0f, 0f, 0.82f));
            }
        }

        public static void ApplyGameOver(Transform root, GameOverOutcome outcome)
        {
            if (root == null) return;

            var panel = root.GetComponent<Image>();
            if (panel != null)
            {
                panel.sprite = BoardVisualUtility.GetPixelCardSprite();
                panel.type = Image.Type.Sliced;
                panel.color = outcome.BackgroundTint;
                panel.raycastTarget = false;
            }

            EnsureEndBackdrop(root, outcome.AccentColor, danger: true);
            EnsureDepthFrame(root, outcome.AccentColor, danger: true);
            EnsureGameOverImpactPlate(root, outcome);
            EnsureAnimator(root, danger: true);

            var title = root.Find("GameOverTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.color = outcome.AccentColor;
                HudTextStyle.ApplyOutlineSafe(title, 0.16f, new Color(0f, 0f, 0f, 0.88f));
            }

            var body = root.Find("GameOverBody")?.GetComponent<TextMeshProUGUI>();
            if (body != null)
            {
                body.color = new Color(0.82f, 0.82f, 0.88f);
                body.raycastTarget = false;
                HudTextStyle.ApplyOutlineSafe(body, 0.10f, new Color(0f, 0f, 0f, 0.72f));
            }

            BringGameOverContentToFront(root);
        }

        /// <summary>演出レイヤーの上にテキスト列・本文・ボタンを重ねる。</summary>
        public static void BringGameOverContentToFront(Transform root)
        {
            if (root == null) return;

            var stage    = root.Find("GameOverVisualStage");
            var impact   = root.Find("GameOverImpactPlate");
            var backdrop = root.Find("GameOverTextBackdrop");
            var accent   = root.Find("AccentPanel");
            var title    = root.Find("GameOverTitle");
            var body     = root.Find("GameOverBody");
            var button   = root.Find("TitleButton");

            int index = 9;
            if (stage != null) stage.SetSiblingIndex(index++);
            if (impact != null) impact.SetSiblingIndex(index++);
            if (backdrop != null) backdrop.SetSiblingIndex(index++);
            if (accent != null) accent.SetSiblingIndex(index++);
            if (title != null) title.SetSiblingIndex(index++);
            if (body != null) body.SetSiblingIndex(index++);
            if (button != null) button.SetSiblingIndex(index++);
        }

        private static void EnsureEndBackdrop(Transform root, Color accent, bool danger)
        {
            var backdrop = EnsureImage(root, "EndSceneDepthBackdrop");
            Stretch(backdrop.rectTransform);
            backdrop.sprite = BoardVisualUtility.GetPixelPlatformBackdropSprite();
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = false;
            backdrop.color = danger
                ? new Color(0.20f, 0.08f, 0.08f, 0.82f)
                : new Color(0.86f, 0.94f, 1f, 0.90f);
            backdrop.transform.SetAsFirstSibling();

            var clouds = EnsureImage(root, "EndSceneCloudParallax");
            Stretch(clouds.rectTransform);
            clouds.sprite = BoardVisualUtility.GetPixelCloudDepthSprite();
            clouds.type = Image.Type.Simple;
            clouds.preserveAspect = false;
            clouds.color = danger
                ? new Color(0.34f, 0.16f, 0.20f, 0.30f)
                : new Color(1f, 1f, 1f, 0.34f);
            clouds.transform.SetSiblingIndex(1);

            var floor = EnsureImage(root, "EndSceneFloorShadow");
            var floorRt = floor.rectTransform;
            floorRt.anchorMin = new Vector2(0.5f, 0f);
            floorRt.anchorMax = new Vector2(0.5f, 0f);
            floorRt.pivot = new Vector2(0.5f, 0.5f);
            floorRt.sizeDelta = new Vector2(940f, 150f);
            floorRt.anchoredPosition = new Vector2(0f, 88f);
            floor.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            floor.type = Image.Type.Simple;
            floor.color = new Color(0f, 0f, 0f, danger ? 0.34f : 0.20f);
            floor.transform.SetSiblingIndex(2);

            var foreground = EnsureImage(root, "EndSceneForegroundParallax");
            Stretch(foreground.rectTransform);
            foreground.sprite = BoardVisualUtility.GetPixelForegroundDepthSprite();
            foreground.type = Image.Type.Simple;
            foreground.preserveAspect = false;
            foreground.color = danger
                ? new Color(0.24f, 0.08f, 0.08f, 0.42f)
                : new Color(1f, 1f, 1f, 0.32f);
            foreground.transform.SetSiblingIndex(3);

            var light = EnsureImage(root, "EndSceneAccentPlate");
            var lightRt = light.rectTransform;
            lightRt.anchorMin = new Vector2(0.5f, 1f);
            lightRt.anchorMax = new Vector2(0.5f, 1f);
            lightRt.pivot = new Vector2(0.5f, 1f);
            lightRt.sizeDelta = new Vector2(760f, 10f);
            lightRt.anchoredPosition = new Vector2(0f, -22f);
            light.sprite = BoardVisualUtility.GetPixelSolidSprite();
            light.type = Image.Type.Sliced;
            light.color = new Color(accent.r, accent.g, accent.b, danger ? 0.82f : 0.74f);
            light.transform.SetSiblingIndex(4);
        }

        private static void EnsureDepthFrame(Transform root, Color accent, bool danger)
        {
            var shadow = EnsureImage(root, "EndScenePanelDepthShadow");
            Stretch(shadow.rectTransform, -12f, -12f);
            shadow.sprite = BoardVisualUtility.GetPixelCardSprite();
            shadow.type = Image.Type.Sliced;
            shadow.color = new Color(0f, 0f, 0f, danger ? 0.34f : 0.24f);
            shadow.transform.SetSiblingIndex(5);

            var right = EnsureImage(root, "EndSceneRightExtrude");
            var rightRt = right.rectTransform;
            rightRt.anchorMin = new Vector2(1f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.sizeDelta = new Vector2(14f, -28f);
            rightRt.anchoredPosition = new Vector2(8f, -8f);
            right.sprite = BoardVisualUtility.GetPixelSolidSprite();
            right.type = Image.Type.Sliced;
            right.color = Color.Lerp(Color.black, accent, danger ? 0.18f : 0.26f);
            right.transform.SetSiblingIndex(6);

            var bottom = EnsureImage(root, "EndSceneBottomExtrude");
            var bottomRt = bottom.rectTransform;
            bottomRt.anchorMin = new Vector2(0f, 0f);
            bottomRt.anchorMax = new Vector2(1f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(-28f, 14f);
            bottomRt.anchoredPosition = new Vector2(8f, -12f);
            bottom.sprite = BoardVisualUtility.GetPixelSolidSprite();
            bottom.type = Image.Type.Sliced;
            bottom.color = Color.Lerp(Color.black, accent, danger ? 0.16f : 0.22f);
            bottom.transform.SetSiblingIndex(7);
        }

        private static void EnsureCareerCard(Transform root, GraduationOutcome? outcome)
        {
            var card = EnsureContainer(root, "CareerDecision3DCard");
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(350f, 142f);
            rt.anchoredPosition = new Vector2(-42f, 42f);
            card.transform.SetSiblingIndex(8);

            var accent = outcome?.AccentColor ?? new Color(0.86f, 0.68f, 0.28f, 1f);
            EnsureCardLayer(card.transform, "CareerCardShadow", new Vector2(12f, -12f),
                new Vector2(350f, 142f), BoardVisualUtility.GetPixelCardSprite(), new Color(0f, 0f, 0f, 0.34f));
            EnsureCardLayer(card.transform, "CareerCardFace", Vector2.zero,
                new Vector2(350f, 142f), BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.16f, 0.20f, 0.28f, 0.94f));
            EnsureCardLayer(card.transform, "CareerCardTopRule", new Vector2(0f, 57f),
                new Vector2(318f, 10f), BoardVisualUtility.GetPixelSolidSprite(), accent);

            var rank = EnsureLabel(card.transform, "CareerRankLabel");
            rank.text = outcome != null ? $"RANK {outcome.Value.Rank}" : "進路決定";
            rank.fontSize = 28f;
            rank.fontStyle = FontStyles.Bold;
            rank.color = accent;
            SetRect(rank.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-28f, 34f), new Vector2(0f, -18f));
            HudTextStyle.ApplyOutlineSafe(rank, 0.14f, new Color(0f, 0f, 0f, 0.80f));

            var path = EnsureLabel(card.transform, "CareerPathLabel");
            path.text = outcome?.CareerPath ?? "修了判定中";
            path.fontSize = 18f;
            path.fontStyle = FontStyles.Bold;
            path.color = Color.white;
            path.enableAutoSizing = true;
            path.fontSizeMin = 12f;
            path.fontSizeMax = 18f;
            SetRect(path.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(-34f, 48f), new Vector2(0f, -6f));
            HudTextStyle.ApplyOutlineSafe(path, 0.10f, new Color(0f, 0f, 0f, 0.78f));

            var sub = EnsureLabel(card.transform, "CareerSubtitleLabel");
            sub.text = outcome?.Subtitle ?? "";
            sub.fontSize = 13f;
            sub.color = new Color(0.82f, 0.88f, 0.94f, 1f);
            SetRect(sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-34f, 26f), new Vector2(0f, 14f));
            HudTextStyle.ApplyOutlineSafe(sub, 0.08f, new Color(0f, 0f, 0f, 0.72f));
        }

        private static void EnsureGameOverImpactPlate(Transform root, GameOverOutcome outcome)
        {
            var plate = EnsureContainer(root, "GameOverImpactPlate");
            var rt = plate.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(310f, 102f);
            rt.anchoredPosition = new Vector2(38f, -42f);
            plate.transform.SetSiblingIndex(8);

            EnsureCardLayer(plate.transform, "GameOverImpactShadow", new Vector2(10f, -10f),
                new Vector2(310f, 102f), BoardVisualUtility.GetPixelCardSprite(), new Color(0f, 0f, 0f, 0.34f));
            EnsureCardLayer(plate.transform, "GameOverImpactFace", Vector2.zero,
                new Vector2(310f, 102f), BoardVisualUtility.GetPixelCardSprite(), new Color(0.11f, 0.08f, 0.08f, 0.94f));
            EnsureCardLayer(plate.transform, "GameOverImpactRule", new Vector2(0f, -38f),
                new Vector2(272f, 8f), BoardVisualUtility.GetPixelSolidSprite(), outcome.AccentColor);

            var label = EnsureLabel(plate.transform, "GameOverImpactLabel");
            label.text = outcome.Headline;
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.color = outcome.AccentColor;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 20f;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-34f, -28f), new Vector2(0f, 7f));
            HudTextStyle.ApplyOutlineSafe(label, 0.14f, new Color(0f, 0f, 0f, 0.86f));
        }

        private static Image EnsureImage(Transform root, string name)
        {
            var child = root.Find(name);
            Image img;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                img = go.GetComponent<Image>();
            }
            else
                img = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();

            img.raycastTarget = false;
            return img;
        }

        private static GameObject EnsureContainer(Transform root, string name)
        {
            var child = root.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root, false);
            return go;
        }

        private static void EnsureCardLayer(Transform parent, string name, Vector2 pos, Vector2 size,
            Sprite sprite, Color color)
        {
            var img = EnsureImage(parent, name);
            img.sprite = sprite;
            img.type = sprite == BoardVisualUtility.GetPixelCardSprite() ? Image.Type.Sliced : Image.Type.Sliced;
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            img.transform.SetAsLastSibling();
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name)
        {
            var child = parent.Find(name);
            TextMeshProUGUI tmp;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            else
                tmp = child.GetComponent<TextMeshProUGUI>() ?? child.gameObject.AddComponent<TextMeshProUGUI>();

            JapaneseFontProvider.Apply(tmp);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.transform.SetAsLastSibling();
            return tmp;
        }

        private static void EnsureAnimator(Transform root, bool danger)
        {
            var animator = root.GetComponent<EndSceneFaux3DAnimator>();
            if (animator == null)
                animator = root.gameObject.AddComponent<EndSceneFaux3DAnimator>();
            animator.Configure(danger);
        }

        private static void Stretch(RectTransform rt, float xOffset = 0f, float yOffset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(xOffset, yOffset);
            rt.offsetMax = new Vector2(-xOffset, -yOffset);
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }
    }
}
