using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §3.1 — メインゲーム HUD / 右下アクション / 上部リソースバーのレイアウト。</summary>
    [DefaultExecutionOrder(-50)]
    public class GameMainLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 actionPanelPadding = new(20f, 20f);
        [SerializeField] private Vector2 actionButtonSize   = new(246f, 60f);

        private const float LegendRowHeight = 18f;
        private const float LegendFontSize = 11f;
        private const float LegendSwatchSize = 13f;

        private void Awake() => ApplyLayout();

        public void ApplyLayout()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null) return;

            UiCanvasUtility.NormalizeCanvasRoot(canvas);
            ResourceHudVisuals.SetupTopResourceBar(canvas.transform);
            LayoutPlayerInfoPanel(canvas.transform);
            LayoutLogPanel(canvas.transform);
            HideSquareLegend(canvas.transform);
            LayoutActionArea(canvas.transform);
            LayoutOverviewButton(canvas.transform);
            LayoutMenuButton(canvas.transform);
            KenneyUiStyler.StyleCanvas(canvas);
            EnsurePresentationDimmer(canvas);
            EnsureFontBootstrap(canvas);
            RestoreModalLayerIfVisible();
        }

        private static void RestoreModalLayerIfVisible()
        {
            if (!EventModalUI.HasVisibleModal) return;
            var modal = EventModalUI.Instance
                ?? Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);
            modal?.EnsureVisibleLayer();
        }

        private static void EnsureFontBootstrap(Canvas canvas)
        {
            if (canvas == null) return;
            if (canvas.GetComponent<GameUIFontBootstrap>() == null)
                canvas.gameObject.AddComponent<GameUIFontBootstrap>();
            if (canvas.GetComponent<BoardOverviewUi>() == null)
                canvas.gameObject.AddComponent<BoardOverviewUi>();
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
                new Color(0.10f, 0.12f, 0.17f, 0.80f));
            GameUiChrome.ApplyAccentRail(panel, new Color(0.86f, 0.68f, 0.28f, 0.82f), 4f);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot     = new Vector2(0f, 1f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = new Vector2(UiSafeLayout.LeftPanelWidth, -UiSafeLayout.TopBarHeight);

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

            // CharacterTypeText はコード管理外のシーン置き去りラベルで、プレイヤー名と
            // 内容が重複したまま盤面中央に浮く。左パネルの PlayerNameText が同じ情報を
            // 表示するため非表示にする。
            var strayType = FindDeep(canvas, "CharacterTypeText");
            if (strayType != null) strayType.gameObject.SetActive(false);

            foreach (var name in new[]
            {
                "PlayerNameText", "HudText", "GoalDistanceText",
                "TuitionDistanceText", "SkipTurnsText", "IgnoreEventsText", "MentalSlider"
            })
            {
                var t = FindDeep(canvas, name);
                if (t != null) t.SetParent(panel.transform, false);
            }

            float y = -14f;
            y = PlaceInfoText(panel.transform, "PlayerNameText",  y, 40f, HudTextStyle.PlayerNameSize, true);
            y = PlaceInfoText(panel.transform, "HudText",         y, 42f, HudTextStyle.JuiceStatusFontSize, true);
            y = PlaceInfoText(panel.transform, "MentalSlider",    y, 22f, 0f, false, new Vector2(314f, 18f));
            y = PlaceInfoText(panel.transform, "GoalDistanceText", y, 31f, HudTextStyle.InfoFontSize, false);
            y = PlaceInfoText(panel.transform, "TuitionDistanceText", y, 31f, HudTextStyle.InfoFontSize, false);
            y = PlaceInfoText(panel.transform, "SkipTurnsText",   y, 28f, HudTextStyle.InfoFontSize, false);
            PlaceInfoText(panel.transform, "IgnoreEventsText", y, 28f, HudTextStyle.InfoFontSize, false);

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
            rt.anchoredPosition = new Vector2(14f, y);
            rt.sizeDelta = sizeOverride ?? new Vector2(314f, height);

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

            var turn = panel.Find("HudText")?.GetComponent<TextMeshProUGUI>();
            if (turn != null)
                HudTextStyle.ApplyReadable(turn, HudTextStyle.JuiceStatusFontSize, new Color(0.55f, 1f, 0.75f), true);

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

            var panel = EnsurePanel(canvas, "LogPanel", new Color(0.10f, 0.12f, 0.17f, 0.66f));
            GameUiChrome.ApplyAccentRail(panel, new Color(0.50f, 0.66f, 0.88f, 0.58f), 3f);
            var panelRt = panel.GetComponent<RectTransform>();
            panel.SetParent(canvas.Find("PlayerInfoPanel") ?? canvas, false);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = Vector2.zero;
            panelRt.offsetMin = new Vector2(UiSafeLayout.Gap, UiSafeLayout.Gap);
            panelRt.offsetMax = new Vector2(-UiSafeLayout.Gap, 170f);

            log.SetParent(panel.transform, false);
            var logRt = log.GetComponent<RectTransform>();
            logRt.anchorMin = Vector2.zero;
            logRt.anchorMax = Vector2.one;
            logRt.offsetMin = new Vector2(10f, 8f);
            logRt.offsetMax = new Vector2(-10f, -8f);

            var tmp = log.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                HudTextStyle.ApplyLog(tmp);
                tmp.alignment = TextAlignmentOptions.BottomLeft;
            }
        }

        private static void HideSquareLegend(Transform canvas)
        {
            var panel = canvas.Find("SquareLegendPanel");
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        private static void LayoutSquareLegend(Transform canvas)
        {
            var panel = EnsurePanel(canvas, "SquareLegendPanel", new Color(0.12f, 0.14f, 0.20f, 0.88f));
            panel.gameObject.SetActive(true);
            GameUiChrome.ApplyAccentRail(panel, new Color(0.56f, 0.78f, 0.58f, 0.78f), 4f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-18f, -ResourceHudVisuals.TopBarHeight - 56f);
            rt.sizeDelta = new Vector2(246f, 372f);

            var v = panel.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 8, 8);
            v.spacing = 3f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fitter = panel.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.enabled = false;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            EnsureLegendTitle(panel, "LegendTitle", "マス凡例");
            EnsureLegendRow(panel, "Legend_Event", SquareType.Event, "イベント", "選択肢");
            EnsureLegendRow(panel, "Legend_Tuition", SquareType.Tuition, "学費", "所持金減");
            EnsureLegendRow(panel, "Legend_Journal", SquareType.Journal, "論文", "IF増");
            EnsureLegendRow(panel, "Legend_Lecture", SquareType.Lecture, "ゼミ", "能力変動");
            EnsureLegendRow(panel, "Legend_Rest", SquareType.Rest, "休息", "メンタル回復");
            EnsureLegendRow(panel, "Legend_PartTime", SquareType.PartTime, "バイト", "所持金増");
            EnsureLegendRow(panel, "Legend_Bonus", SquareType.Bonus, "チャンス", "良効果");
            EnsureLegendRow(panel, "Legend_Penalty", SquareType.Penalty, "ペナルティ", "悪効果");
            EnsureLegendTitle(panel, "LegendEventTitle", "イベント色");
            EnsureTagLegendRow(panel, "LegendTag_Trouble", "トラブル", "赤: 事故/負荷");
            EnsureTagLegendRow(panel, "LegendTag_Urgent", "緊急", "橙: 即対応");
            EnsureTagLegendRow(panel, "LegendTag_Research", "研究", "青: 研究進行");
            EnsureTagLegendRow(panel, "LegendTag_Conference", "学会", "紺: 学会関連");
            EnsureTagLegendRow(panel, "LegendTag_Life", "生活", "緑: 生活/回復");
            EnsureTagLegendRow(panel, "LegendTag_Professor", "教授", "紫: 教授対応");
        }

        private static void EnsureLegendTitle(Transform panel, string name, string label)
        {
            var title = panel.Find(name);
            if (title == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(panel, false);
                title = go.transform;
            }

            var tmp = title.GetComponent<TextMeshProUGUI>() ?? title.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            HudTextStyle.ApplyReadable(tmp, HudTextStyle.Scale(16f), new Color(1f, 0.95f, 0.74f), true);
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);

            var le = title.GetComponent<LayoutElement>() ?? title.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = LegendRowHeight;
        }

        private static void EnsureLegendRow(Transform panel, string name, SquareType type, string label, string effect)
        {
            var row = panel.Find(name);
            if (row == null)
            {
                var rowGo = new GameObject(name, typeof(RectTransform));
                rowGo.transform.SetParent(panel, false);
                row = rowGo.transform;
            }

            var h = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = LegendRowHeight;

            var swatch = row.Find("Swatch");
            if (swatch == null)
            {
                var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(row, false);
                swatch = go.transform;
            }

            var swatchRt = swatch.GetComponent<RectTransform>();
            swatchRt.sizeDelta = new Vector2(LegendSwatchSize, LegendSwatchSize);
            var img = swatch.GetComponent<Image>() ?? swatch.gameObject.AddComponent<Image>();
            img.color = EventTagColors.GetSquareTypePanelColor(type);
            img.raycastTarget = false;
            var swatchLe = swatch.GetComponent<LayoutElement>() ?? swatch.gameObject.AddComponent<LayoutElement>();
            swatchLe.preferredWidth = LegendSwatchSize;
            swatchLe.preferredHeight = LegendSwatchSize;

            var text = row.Find("Text");
            if (text == null)
            {
                var go = new GameObject("Text", typeof(RectTransform));
                go.transform.SetParent(row, false);
                text = go.transform;
            }

            var tmp = text.GetComponent<TextMeshProUGUI>() ?? text.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{label}: {effect}";
            HudTextStyle.ApplyReadable(tmp, LegendFontSize, new Color(0.9f, 0.93f, 1f), false);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            JapaneseFontProvider.Apply(tmp);

            var textLe = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;
            textLe.preferredWidth = 194f;
        }

        private static void EnsureTagLegendRow(Transform panel, string name, string tag, string label)
        {
            var row = panel.Find(name);
            if (row == null)
            {
                var rowGo = new GameObject(name, typeof(RectTransform));
                rowGo.transform.SetParent(panel, false);
                row = rowGo.transform;
            }

            var h = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = LegendRowHeight;

            var swatch = row.Find("Swatch");
            if (swatch == null)
            {
                var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(row, false);
                swatch = go.transform;
            }

            var swatchRt = swatch.GetComponent<RectTransform>();
            swatchRt.sizeDelta = new Vector2(LegendSwatchSize, LegendSwatchSize);
            var img = swatch.GetComponent<Image>() ?? swatch.gameObject.AddComponent<Image>();
            img.color = EventTagColors.GetPanelColor(new[] { tag });
            img.raycastTarget = false;
            var swatchLe = swatch.GetComponent<LayoutElement>() ?? swatch.gameObject.AddComponent<LayoutElement>();
            swatchLe.preferredWidth = LegendSwatchSize;
            swatchLe.preferredHeight = LegendSwatchSize;

            var text = row.Find("Text");
            if (text == null)
            {
                var go = new GameObject("Text", typeof(RectTransform));
                go.transform.SetParent(row, false);
                text = go.transform;
            }

            var tmp = text.GetComponent<TextMeshProUGUI>() ?? text.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            HudTextStyle.ApplyReadable(tmp, LegendFontSize, new Color(0.9f, 0.93f, 1f), false);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            JapaneseFontProvider.Apply(tmp);

            var textLe = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;
            textLe.preferredWidth = 194f;
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
            GameUiChrome.ApplySurface(panel, bgColor);
            return panel;
        }

        private void LayoutActionArea(Transform canvas)
        {
            Transform panel = canvas.Find("BottomActionBar") ?? canvas.Find("ActionPanel");
            if (panel == null)
            {
                var panelGo = new GameObject("ActionPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvas, false);
                panel = panelGo.transform;
            }

            var roll  = FindDeep(canvas, "RollButton");
            var skill = FindDeep(canvas, "SkillButton");
            var dice  = FindDeep(canvas, "DiceResult");
            var reroll = FindDeep(canvas, "ItemButton_DiceReroll");
            var heal = FindDeep(canvas, "ItemButton_MentalHeal");
            var bonus = FindDeep(canvas, "ItemButton_MoneyBonus");
            if (roll == null && skill == null && dice == null) return;

            var panelRt = panel.GetComponent<RectTransform>();
            panel.name = "BottomActionBar";
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = Vector2.zero;
            panelRt.offsetMin = new Vector2(UiSafeLayout.LeftPanelWidth + UiSafeLayout.Gap, UiSafeLayout.Gap);
            panelRt.offsetMax = new Vector2(-UiSafeLayout.Gap, UiSafeLayout.BottomBarHeight - UiSafeLayout.Gap);
            GameUiChrome.ApplySurface(panel, new Color(0.10f, 0.12f, 0.18f, 0.88f));
            GameUiChrome.ApplyAccentRail(panel, new Color(0.86f, 0.68f, 0.28f, 0.82f), 4f);
            if (!panel.TryGetComponent<CanvasGroup>(out var cg) || cg == null)
                cg = panel.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.92f;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var v = panel.GetComponent<VerticalLayoutGroup>();
            if (v != null) v.enabled = false;
            var fitter = panel.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;

            if (roll != null)
            {
                roll.SetParent(panel, false);
                roll.SetAsLastSibling();
                LayoutActionChild(roll, new Vector2(-8f, -18f), new Vector2(246f, 60f));
                SetupActionButton(roll, "ダイスを振る", true);
            }
            if (skill != null)
            {
                skill.SetParent(panel, false);
                skill.SetAsLastSibling();
                LayoutActionChild(skill, new Vector2(-266f, -18f), new Vector2(190f, 60f));
                SetupActionButton(skill, "ワザ", false);
            }

            if (dice != null)
            {
                dice.SetParent(panel, false);
                dice.SetAsFirstSibling();
                LayoutActionChild(dice, new Vector2(-468f, -32f), new Vector2(190f, 32f));
                var le = dice.GetComponent<LayoutElement>() ?? dice.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
                var tmp = dice.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.alignment = TextAlignmentOptions.MidlineRight;
                    HudTextStyle.ApplyReadable(tmp, HudTextStyle.Scale(16f), Color.white, true);
                    tmp.margin = new Vector4(44f, 0f, 4f, 0f);
                }

                LayoutDiceIcon(dice);
            }

            LayoutLeftActionChild(reroll, panel, 326f);
            LayoutLeftActionChild(heal, panel, 488f);
            LayoutLeftActionChild(bonus, panel, 650f);

            if (!EventModalUI.HasVisibleModal)
                panel.SetAsLastSibling();
        }

        private static void LayoutActionChild(Transform child, Vector2 anchoredPosition, Vector2 size)
        {
            if (child == null) return;
            var rt = child.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
        }

        private static void LayoutLeftActionChild(Transform child, Transform panel, float x)
        {
            if (child == null || panel == null) return;
            child.SetParent(panel, false);
            var rt = child.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(150f, 60f);
            var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // 「気分転換ドリンク ×0」のような長いラベルが 2 行に折り返さないよう、
            // 1 行維持のまま幅に収まるフォントへ自動縮小する。
            var label = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 11f;
                if (label.fontSizeMax < label.fontSize) label.fontSizeMax = label.fontSize;
            }
        }

        private static void LayoutDiceIcon(Transform dice)
        {
            if (dice == null) return;
            var icon = FindDeep(dice, "DiceIcon");
            if (icon == null) return;

            icon.SetParent(dice, false);
            var rt = icon.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(4f, 0f);
            rt.sizeDelta = new Vector2(32f, 32f);
            icon.SetAsFirstSibling();

            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                img.preserveAspect = true;
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
                HudTextStyle.ApplyReadable(labelT, HudTextStyle.Scale(17f), Color.white, true);
                if (primary)
                {
                    labelT.color = GameUiChrome.PrimaryText;
                    HudTextStyle.ApplyOutlineSafe(labelT, 0f, Color.clear);
                }
            }
        }

        private static void LayoutOverviewButton(Transform canvas)
        {
            var btn = canvas.Find("OverviewButton");
            if (btn == null)
            {
                var go = new GameObject("OverviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas, false);
                btn = go.transform;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(btn, false);
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "全体を見る";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                HudTextStyle.ApplyReadable(tmp, HudTextStyle.Scale(15f), Color.white, true);
                JapaneseFontProvider.Apply(tmp);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            }

            var actionBar = canvas.Find("BottomActionBar") ?? canvas.Find("ActionPanel");
            btn.SetParent(actionBar ?? canvas, false);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(UiSafeLayout.Gap, 0f);
            rt.sizeDelta = new Vector2(150f, 60f);
            var layout = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.14f, 0.18f, 0.26f, 0.92f);
                img.raycastTarget = true;
            }

            var button = btn.GetComponent<Button>();
            if (button != null)
                GameUiChrome.ApplyChoiceButton(button, true);

            btn.SetAsLastSibling();
        }

        private static void LayoutMenuButton(Transform canvas)
        {
            var menu = canvas.Find("MenuButton");
            if (menu == null) return;
            var actionBar = canvas.Find("BottomActionBar") ?? canvas.Find("ActionPanel");
            menu.SetParent(actionBar ?? canvas, false);
            var rt = menu.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(174f, 0f);
            rt.sizeDelta = new Vector2(132f, 60f);
            var layout = menu.GetComponent<LayoutElement>() ?? menu.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            menu.SetAsLastSibling();
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
