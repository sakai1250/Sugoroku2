using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>
    /// 遊び方ヘルプ。タイトル画面・ポーズメニューから開くコード生成オーバーレイ。
    /// 目的・操作・ステータス・マスの種類をまとめて案内する。
    /// </summary>
    public class HelpPanelUI : MonoBehaviour
    {
        private static HelpPanelUI _instance;

        private GameObject _overlay;
        private RectTransform _panelRt;
        private CanvasGroup _group;
        private Image _dimmer;
        private Coroutine _transitionCo;
        private GameObject _selectionBeforeOpen;

        /// <summary>ヘルプが表示中かどうか。ポーズのESC処理等で参照される。</summary>
        public static bool IsOpen =>
            _instance != null && _instance._overlay != null && _instance._overlay.activeSelf;

        public static void Open()
        {
            EnsureInstance();
            _instance.Show();
        }

        public static void Close()
        {
            if (_instance != null) _instance.Hide();
        }

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("HelpPanelUI");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HelpPanelUI>();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        private void Show()
        {
            if (_overlay == null) _overlay = BuildOverlay();
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            _overlay.SetActive(true);
            if (_group != null) _group.alpha = 0f;
            if (gameObject.activeInHierarchy)
                _transitionCo = StartCoroutine(Entrance());
            var es = UnityEngine.EventSystems.EventSystem.current;
            var closeBtn = _overlay != null ? _overlay.transform.Find("HelpPanel/CloseButton") : null;
            if (es != null)
            {
                // WCAG 2.4.3: 閉じた時にフォーカスを元の場所へ返すため、開く前の選択を記憶する
                _selectionBeforeOpen = es.currentSelectedGameObject;
                if (closeBtn != null)
                    es.SetSelectedGameObject(closeBtn.gameObject);
            }
        }

        private void Hide()
        {
            if (_overlay == null || !_overlay.activeSelf) return;
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            if (gameObject.activeInHierarchy)
                _transitionCo = StartCoroutine(Exit());
            else
                _overlay.SetActive(false);
        }

        private IEnumerator Exit()
        {
            float duration = GameConfig.AnimationDuration(0.12f);
            float startAlpha = _group != null ? _group.alpha : 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float f = JuiceMath.EaseOutCubic(t);
                if (_group != null) _group.alpha = Mathf.Lerp(startAlpha, 0f, f);
                if (_dimmer != null) _dimmer.color = new Color(0f, 0f, 0f, 0.55f * (1f - f));
                yield return null;
            }
            if (_overlay != null) _overlay.SetActive(false);
            if (_group != null) _group.alpha = 1f;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null)
            {
                // 開く前に選択していた要素へフォーカスを返す（破棄・非活動時は解放）
                var restore = _selectionBeforeOpen;
                _selectionBeforeOpen = null;
                if (restore != null && restore.activeInHierarchy)
                    es.SetSelectedGameObject(restore);
                else
                    es.SetSelectedGameObject(null);
            }
            _transitionCo = null;
        }

        private IEnumerator Entrance()
        {
            float duration = GameConfig.AnimationDuration(0.18f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = JuiceMath.EaseOutCubic(t);
                if (_group != null) _group.alpha = e;
                if (_dimmer != null) _dimmer.color = new Color(0f, 0f, 0f, 0.55f * e);
                if (_panelRt != null)
                    _panelRt.localScale = Vector3.Lerp(new Vector3(1.04f, 1.04f, 1f), Vector3.one, e);
                yield return null;
            }
            if (_group != null) _group.alpha = 1f;
            if (_dimmer != null) _dimmer.color = new Color(0f, 0f, 0f, 0.55f);
            if (_panelRt != null) _panelRt.localScale = Vector3.one;
        }

        private GameObject BuildOverlay()
        {
            var root = new GameObject("HelpOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = UiLayerManager.SortHelpPanel;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiSafeLayout.ReferenceWidth, UiSafeLayout.ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            var rootRt = root.GetComponent<RectTransform>();
            UiLayerManager.Stretch(rootRt);

            var dimmer = CreateImage(root.transform, "Dimmer", new Color(0f, 0f, 0f, 0.55f));
            dimmer.rectTransform.anchorMin = Vector2.zero;
            dimmer.rectTransform.anchorMax = Vector2.one;
            dimmer.rectTransform.offsetMin = Vector2.zero;
            dimmer.rectTransform.offsetMax = Vector2.zero;
            dimmer.raycastTarget = true;
            _dimmer = dimmer;

            var panel = new GameObject("HelpPanel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            _panelRt = panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRt.pivot = new Vector2(0.5f, 0.5f);
            _panelRt.anchoredPosition = Vector2.zero;
            _panelRt.sizeDelta = new Vector2(1080f, 860f);
            GameUiChrome.ApplySurface(_panelRt, new Color(0.12f, 0.14f, 0.20f, 0.98f));
            GameUiChrome.ApplyAccentRail(_panelRt, new Color(0.86f, 0.68f, 0.28f, 0.88f), 6f);
            var panelImg = panel.GetComponent<Image>();
            panelImg.raycastTarget = true;

            _group = panel.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            var title = CreateText(panel.transform, "Title", "遊び方", HudTextStyle.Scale(34f), TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.70f, 1f), FontStyles.Bold);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-32f, 48f);

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.color = GameUiChrome.SurfaceRaised;
            var closeBtn = closeGo.GetComponent<Button>();
            GameUiChrome.ApplyButton(closeBtn, primary: false);
            var closeLabel = CreateText(closeGo.transform, "Label", "×", HudTextStyle.Scale(26f), TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            var closeLabelRt = closeLabel.rectTransform;
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = closeLabelRt.offsetMax = Vector2.zero;
            UiSafeLayout.LayoutCloseButton(panel.transform, closeGo.transform);
            closeBtn.onClick.AddListener(Hide);

            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(32f, 24f);
            scrollRt.offsetMax = new Vector2(-32f, -72f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollRt, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-20f, 0f);
            var viewportImg = viewport.GetComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.20f);
            viewportImg.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewportRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var contentV = content.AddComponent<VerticalLayoutGroup>();
            contentV.padding = new RectOffset(20, 20, 16, 20);
            contentV.spacing = 16f;
            contentV.childAlignment = TextAnchor.UpperLeft;
            contentV.childControlWidth = true;
            contentV.childControlHeight = false;
            contentV.childForceExpandWidth = true;
            contentV.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            UiSafeLayout.CreateScrollbar(scroll, viewportRt, new Color(0.86f, 0.68f, 0.28f, 0.55f));

            AddSection(contentRt, "このゲームについて", new[]
            {
                "サイコロを振ってコマを進め、ゴールに到達すると「修了」です。",
                "最終的に一番スコアが高い人が優勝。スコアは IF・所持金・メンタル・徳から計算されます。",
                "所持金が 0 になるか、メンタルが 0 になると退学(脱落)してしまいます。",
            });

            AddSection(contentRt, "操作", new[]
            {
                "右下の「ダイスを振る」ボタンでサイコロが回ります。",
                "ワザを使うと、そのターンのサイコロが自動で回ります（振る手間はありません）。",
                "イベントでは「選択肢」から 1 つ選んでください。",
                "ゲーム中は ESC キーでポーズメニューを開けます。",
            });

            AddStatRows(contentRt);

            AddSection(contentRt, "ワザとアイテム", new[]
            {
                "ワザは、サイコロを振る前の HUD 下段「ワザ」ボタンで使えます（1ターンに1回だけ）。",
                "効果はキャラクター選択画面と、ポーズメニューの「ステータス詳細」で確認できます。",
                "アイテムはマスやイベントで手に入ります。もう一振り券=サイコロのやり直し、気分転換ドリンク=メンタル回復、臨時収入券=お金獲得。",
                "「回避N」は次のイベントを N 回スキップできる状態です。",
                "「休み×N」は N ターン休みになる状態です。",
                "イベントのタグ横に「◎相性」と出たら、自分のクラスと相性の良いイベントです（良い変動が増えます）。",
            });

            AddSquareLegend(contentRt);

            int sThreshold = DifficultyRules.RankSThreshold(GameSession.Difficulty);
            AddSection(contentRt, "勝敗とランク", new[]
            {
                $"最終スコアが一番高い人が優勝です（脱落した人がいなければ）。",
                $"スコア計算: IF×100 + 所持金×0.5 + 徳×10 + メンタル×2。",
                $"ランクはスコアで決まります。S: {sThreshold}pt以上 / A: 500pt以上（IF15以上なら400pt）/ B: 300pt以上 / C: それ未満。",
                $"ランクSでの修了が最上位の「教授エンド」、ランクA以上は実績「博士への道」の対象です。",
            });

            return root;
        }

        private void AddSection(RectTransform content, string heading, string[] lines)
        {
            var h = CreateText(content, $"Section_{heading}", heading, HudTextStyle.Scale(22f), TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.28f, 1f), FontStyles.Bold);
            h.rectTransform.sizeDelta = new Vector2(0f, 32f);

            var bodyGo = new GameObject($"Body_{heading}", typeof(RectTransform));
            bodyGo.transform.SetParent(content, false);
            var bodyV = bodyGo.AddComponent<VerticalLayoutGroup>();
            bodyV.padding = new RectOffset(8, 8, 4, 4);
            bodyV.spacing = 6f;
            bodyV.childAlignment = TextAnchor.UpperLeft;
            bodyV.childControlWidth = true;
            bodyV.childControlHeight = false;
            bodyV.childForceExpandWidth = true;
            bodyV.childForceExpandHeight = false;
            var bodyFit = bodyGo.AddComponent<ContentSizeFitter>();
            bodyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var line in lines)
            {
                var t = CreateText(bodyGo.transform, "Line", line, HudTextStyle.Scale(17f), TextAlignmentOptions.Left, new Color(0.85f, 0.88f, 0.95f, 1f), FontStyles.Normal);
                t.textWrappingMode = TextWrappingModes.Normal;
                t.overflowMode = TextOverflowModes.Overflow;
                var le = t.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 26f;
                le.flexibleWidth = 1f;
            }
        }

        private void AddStatRows(RectTransform content)
        {
            var heading = CreateText(content, "Section_ステータス", "ステータス", HudTextStyle.Scale(22f), TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.28f, 1f), FontStyles.Bold);
            heading.rectTransform.sizeDelta = new Vector2(0f, 32f);

            AddKeyValueRow(content, "所持金", "研究費・生活費。0 になると破産で退学。", ResourceHudVisuals.MoneyTextColor);
            AddKeyValueRow(content, "IF", "研究の実績値。スコアへの影響が最も大きい指標。", ResourceHudVisuals.IfTextColor);
            AddKeyValueRow(content, "メンタル", "心の余裕。0 になると力尽きて失踪。", ResourceHudVisuals.MentalTextColor);
            AddKeyValueRow(content, "徳", "日頃の善行ポイント。ピンチの時に救済してくれることも。", ResourceHudVisuals.VirtueTextColor);
        }

        private void AddKeyValueRow(RectTransform content, string key, string value, Color accent)
        {
            var rowGo = new GameObject($"Row_{key}", typeof(RectTransform));
            rowGo.transform.SetParent(content, false);
            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 3, 3);
            row.spacing = 14f;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var rowFit = rowGo.AddComponent<ContentSizeFitter>();
            rowFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keyText = CreateText(rowGo.transform, "Key", key, HudTextStyle.Scale(18f), TextAlignmentOptions.Left, accent, FontStyles.Bold);
            var keyRt = keyText.rectTransform;
            keyRt.sizeDelta = new Vector2(130f, 28f);

            var valueText = CreateText(rowGo.transform, "Value", value, HudTextStyle.Scale(17f), TextAlignmentOptions.Left, new Color(0.85f, 0.88f, 0.95f, 1f), FontStyles.Normal);
            valueText.textWrappingMode = TextWrappingModes.Normal;
            valueText.overflowMode = TextOverflowModes.Overflow;
            var valueRt = valueText.rectTransform;
            valueRt.sizeDelta = new Vector2(810f, 28f);
        }

        private void AddSquareLegend(RectTransform content)
        {
            var heading = CreateText(content, "Section_マスの種類", "マスの種類", HudTextStyle.Scale(22f), TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.28f, 1f), FontStyles.Bold);
            heading.rectTransform.sizeDelta = new Vector2(0f, 32f);

            foreach (var (type, note) in SquareLegend)
            {
                var rowGo = new GameObject($"MasuRow_{type}", typeof(RectTransform));
                rowGo.transform.SetParent(content, false);
                var row = rowGo.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(8, 8, 3, 3);
                row.spacing = 12f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = false;
                row.childControlHeight = false;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                var rowFit = rowGo.AddComponent<ContentSizeFitter>();
                rowFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var swatchGo = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatchGo.transform.SetParent(rowGo.transform, false);
                var swatchImg = swatchGo.GetComponent<Image>();
                swatchImg.color = EventTagColors.GetSquareTypePanelColor(type);
                swatchImg.raycastTarget = false;
                var swatchRt = swatchImg.rectTransform;
                swatchRt.sizeDelta = new Vector2(26f, 26f);

                var label = CreateText(rowGo.transform, "Label", $"{SquareEffectLabels.Get(type)} — {note}", HudTextStyle.Scale(17f), TextAlignmentOptions.Left, new Color(0.85f, 0.88f, 0.95f, 1f), FontStyles.Normal);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                var labelRt = label.rectTransform;
                labelRt.sizeDelta = new Vector2(880f, 28f);
            }
        }

        private static readonly (SquareType type, string note)[] SquareLegend =
        {
            (SquareType.Start,   "出発地点"),
            (SquareType.Normal,  "何も起きないマス"),
            (SquareType.Event,   "ランダムイベントが発生"),
            (SquareType.Tuition, "学費を支払う"),
            (SquareType.Journal, "論文投稿で IF を獲得"),
            (SquareType.Lecture, "ゼミ・講義で IF を獲得"),
            (SquareType.Rest,    "メンタルが回復"),
            (SquareType.PartTime,"バイトで収入を得る"),
            (SquareType.Bonus,   "良いことが起きるかも"),
            (SquareType.Penalty, "ペナルティを受ける"),
            (SquareType.Goal,    "ゴール！修了"),
            (SquareType.Branch,  "進路の分岐点"),
        };

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
            TextAlignmentOptions align, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            return tmp;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
