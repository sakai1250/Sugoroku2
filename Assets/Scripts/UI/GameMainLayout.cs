using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.UI
{
    /// <summary>screen.md §3.1 — メインゲーム HUD / 右下アクション / 上部リソースバーのレイアウト。</summary>
    [DefaultExecutionOrder(-50)]
    public class GameMainLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 actionPanelPadding = new(24f, 24f);
        [SerializeField] private Vector2 actionButtonSize   = new(240f, 56f);

        private void Awake() => ApplyLayout();

        public void ApplyLayout()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null) return;

            UiCanvasUtility.NormalizeCanvasRoot(canvas);
            ResourceHudVisuals.SetupTopResourceBar(canvas.transform);
            LayoutPlayerInfoPanel(canvas.transform);
            LayoutActionArea(canvas.transform);
            LayoutMenuButton(canvas.transform);
            LayoutLogPanel(canvas.transform);
            KenneyUiStyler.StyleCanvas(canvas);
            EnsurePresentationDimmer(canvas);
            EnsureFontBootstrap(canvas);
        }

        private static void EnsureFontBootstrap(Canvas canvas)
        {
            if (canvas == null) return;
            if (canvas.GetComponent<GameUIFontBootstrap>() == null)
                canvas.gameObject.AddComponent<GameUIFontBootstrap>();
        }

        private static void EnsurePresentationDimmer(Canvas canvas)
        {
            if (canvas == null) return;
            if (canvas.GetComponent<GameWorldPresentationDimmer>() == null)
                canvas.gameObject.AddComponent<GameWorldPresentationDimmer>();
        }

        private static void LayoutPlayerInfoPanel(Transform canvas)
        {
            var panel = EnsurePanel(canvas, "PlayerInfoPanel",
                new Color(0.04f, 0.06f, 0.11f, 0.88f));
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot     = new Vector2(0f, 1f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = new Vector2(360f, -ResourceHudVisuals.TopBarHeight);

            var hud = canvas.Find("GameHUD");
            if (hud != null)
            {
                var hudImg = hud.GetComponent<Image>();
                if (hudImg != null) hudImg.enabled = false;

                var hudRt = hud.GetComponent<RectTransform>();
                hudRt.anchorMin = Vector2.zero;
                hudRt.anchorMax = Vector2.one;
                hudRt.offsetMin = hudRt.offsetMax = Vector2.zero;
            }

            foreach (var name in new[]
            {
                "PlayerNameText", "TurnStateText", "GoalDistanceText",
                "TuitionDistanceText", "SkipTurnsText", "IgnoreEventsText", "MentalSlider"
            })
            {
                var t = FindDeep(canvas, name);
                if (t != null) t.SetParent(panel.transform, false);
            }

            float y = -16f;
            y = PlaceInfoText(panel.transform, "PlayerNameText",  y, 34f, HudTextStyle.PlayerNameSize, true);
            y = PlaceInfoText(panel.transform, "TurnStateText",   y, 28f, HudTextStyle.InfoFontSize + 2f, true);
            y = PlaceInfoText(panel.transform, "MentalSlider",    y, 22f, 0f, false, new Vector2(320f, 20f));
            y = PlaceInfoText(panel.transform, "GoalDistanceText", y, 26f, HudTextStyle.InfoFontSize, false);
            y = PlaceInfoText(panel.transform, "TuitionDistanceText", y, 26f, HudTextStyle.InfoFontSize, false);
            y = PlaceInfoText(panel.transform, "SkipTurnsText",   y, 24f, HudTextStyle.InfoFontSize, false);
            PlaceInfoText(panel.transform, "IgnoreEventsText", y, 24f, HudTextStyle.InfoFontSize, false);

            StyleInfoTexts(panel.transform);
        }

        private static float PlaceInfoText(Transform panel, string name, float y, float height,
            float fontSize, bool bold, Vector2? sizeOverride = null)
        {
            var t = panel.Find(name);
            if (t == null) return y;

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, y);
            rt.sizeDelta = sizeOverride ?? new Vector2(320f, height);

            if (fontSize > 0f)
            {
                var tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    HudTextStyle.ApplyReadable(tmp, fontSize, Color.white, bold);
            }

            var slider = t.GetComponent<Slider>();
            if (slider != null) KenneyUiStyler.StyleSlider(slider);

            return y - height - 8f;
        }

        private static void StyleInfoTexts(Transform panel)
        {
            var name = panel.Find("PlayerNameText")?.GetComponent<TextMeshProUGUI>();
            if (name != null)
                HudTextStyle.ApplyReadable(name, HudTextStyle.PlayerNameSize, new Color(1f, 0.95f, 0.7f), true);

            var turn = panel.Find("TurnStateText")?.GetComponent<TextMeshProUGUI>();
            if (turn != null)
                HudTextStyle.ApplyReadable(turn, HudTextStyle.InfoFontSize + 2f, new Color(0.55f, 1f, 0.75f), true);

            foreach (var n in new[] { "GoalDistanceText", "TuitionDistanceText", "SkipTurnsText", "IgnoreEventsText" })
            {
                var tmp = panel.Find(n)?.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    HudTextStyle.ApplyInfo(tmp, new Color(0.88f, 0.92f, 1f));
            }
        }

        private static void LayoutLogPanel(Transform canvas)
        {
            var log = FindDeep(canvas, "LogText");
            if (log == null) return;

            var panel = EnsurePanel(canvas, "LogPanel", new Color(0.04f, 0.05f, 0.1f, 0.85f));
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0.42f, 0.28f);
            panelRt.offsetMin = new Vector2(12f, 12f);
            panelRt.offsetMax = new Vector2(-8f, -8f);

            log.SetParent(panel.transform, false);
            var logRt = log.GetComponent<RectTransform>();
            logRt.anchorMin = Vector2.zero;
            logRt.anchorMax = Vector2.one;
            logRt.offsetMin = new Vector2(12f, 10f);
            logRt.offsetMax = new Vector2(-12f, -10f);

            var tmp = log.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                HudTextStyle.ApplyLog(tmp);
                tmp.alignment = TextAlignmentOptions.BottomLeft;
            }
        }

        private static Transform EnsurePanel(Transform canvas, string name, Color bgColor)
        {
            var panel = canvas.Find(name);
            if (panel == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas, false);
                panel = go.transform;
            }

            var img = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return panel;
        }

        private void LayoutActionArea(Transform canvas)
        {
            var roll  = canvas.Find("RollButton");
            var skill = canvas.Find("SkillButton");
            if (roll == null && skill == null) return;

            Transform panel = canvas.Find("ActionPanel");
            if (panel == null)
            {
                var panelGo = new GameObject("ActionPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvas, false);
                panel = panelGo.transform;
            }

            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot     = new Vector2(1f, 0f);
            panelRt.anchoredPosition = -actionPanelPadding;
            panelRt.sizeDelta = new Vector2(280f, 200f);

            var v = panel.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12f;
            v.childAlignment = TextAnchor.LowerRight;
            v.childControlWidth  = false;
            v.childControlHeight = false;
            v.childForceExpandWidth  = false;
            v.childForceExpandHeight = false;

            if (roll != null)
            {
                roll.SetParent(panel, false);
                SetupActionButton(roll, "ダイスを振る", true);
            }
            if (skill != null)
            {
                skill.SetParent(panel, false);
                SetupActionButton(skill, "ワザ", false);
            }

            var dice = canvas.Find("DiceResult");
            if (dice != null)
            {
                dice.SetParent(panel, false);
                var diceRt = dice.GetComponent<RectTransform>();
                diceRt.anchorMin = new Vector2(0f, 1f);
                diceRt.anchorMax = new Vector2(1f, 1f);
                diceRt.pivot     = new Vector2(0.5f, 1f);
                diceRt.anchoredPosition = new Vector2(0f, -4f);
                diceRt.sizeDelta = new Vector2(0f, 44f);
                var le = dice.GetComponent<LayoutElement>() ?? dice.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 44f;
                le.flexibleWidth   = 1f;
                var tmp = dice.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    HudTextStyle.ApplyReadable(tmp, 24f, Color.white, true);
            }
        }

        private void SetupActionButton(Transform btn, string label, bool primary)
        {
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = actionButtonSize;
            var le = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth  = actionButtonSize.x;
            le.preferredHeight = actionButtonSize.y;
            var b = btn.GetComponent<Button>();
            if (b != null) KenneyUiStyler.StyleButton(b, primary);

            var labelT = btn.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (labelT != null)
            {
                labelT.text = label;
                HudTextStyle.ApplyReadable(labelT, 22f, Color.white, true);
            }
        }

        private static void LayoutMenuButton(Transform canvas)
        {
            var menu = canvas.Find("MenuButton");
            if (menu == null) return;
            var rt = menu.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -ResourceHudVisuals.TopBarHeight - 12f);
            rt.sizeDelta = new Vector2(120f, 44f);
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
    }
}
