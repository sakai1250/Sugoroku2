using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Game;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject      _panel;
        [SerializeField] private TextMeshProUGUI _statusTitleText;
        [SerializeField] private TextMeshProUGUI _scoreBreakdownText;
        [SerializeField] private Button          _resumeButton;
        [SerializeField] private Button          _titleButton;
        [SerializeField] private Button          _helpButton;

        private bool _open;
        private Coroutine _transitionCo;
        private bool _titleConfirmArmed;
        private Coroutine _titleConfirmResetCo;
        private string _titleButtonOriginalLabel;

        private void Start()
        {
            _panel ??= gameObject.name == "StatusPanel" ? gameObject : UiBindingUtility.FindObject("StatusPanel");
            _statusTitleText ??= transform.Find("StatusTitle")?.GetComponent<TextMeshProUGUI>();
            _scoreBreakdownText ??= transform.Find("ScoreBreakdownText")?.GetComponent<TextMeshProUGUI>();
            _resumeButton ??= transform.Find("ResumeButton")?.GetComponent<Button>();
            _titleButton ??= transform.Find("TitleButton")?.GetComponent<Button>();
            _helpButton ??= transform.Find("HelpButton")?.GetComponent<Button>();
            if (_helpButton == null) _helpButton = CreateHelpButton();
            ApplyChrome();
            ApplySafeLayout();
            if (_panel != null) _panel.SetActive(false);
            _resumeButton?.onClick.AddListener(Close);
            _titleButton?.onClick.AddListener(GoToTitle);
            _helpButton?.onClick.AddListener(HelpPanelUI.Open);
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            Time.timeScale = 0f;
            if (_panel != null)
            {
                _panel.SetActive(true);
                var cg = _panel.GetComponent<CanvasGroup>() ?? _panel.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
                _panel.transform.localScale = new Vector3(1.04f, 1.04f, 1f);
            }
            RefreshScore();
            SetInitialSelection();
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            _transitionCo = StartCoroutine(OpenTransition());
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            DisarmTitleConfirm();
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            _transitionCo = StartCoroutine(CloseTransition());
        }

        private System.Collections.IEnumerator OpenTransition()
        {
            var cg = _panel != null ? _panel.GetComponent<CanvasGroup>() : null;
            float duration = GameConfig.AnimationDuration(0.15f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float f = JuiceMath.EaseOutCubic(t);
                if (cg != null) cg.alpha = f;
                if (_panel != null)
                    _panel.transform.localScale = Vector3.Lerp(new Vector3(1.04f, 1.04f, 1f), Vector3.one, f);
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
            if (_panel != null) _panel.transform.localScale = Vector3.one;
            _transitionCo = null;
        }

        private System.Collections.IEnumerator CloseTransition()
        {
            var cg = _panel != null ? _panel.GetComponent<CanvasGroup>() : null;
            float duration = GameConfig.AnimationDuration(0.12f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float f = JuiceMath.EaseOutCubic(t);
                if (cg != null) cg.alpha = 1f - f;
                if (_panel != null)
                    _panel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.97f, 0.97f, 1f), f);
                yield return null;
            }
            if (_panel != null) _panel.SetActive(false);
            Time.timeScale = 1f;
            ClearSelection();
            _transitionCo = null;
        }

        /// <summary>ポーズを開いたら「再開」ボタンにフォーカスを移す。</summary>
        private void SetInitialSelection()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(_resumeButton != null ? _resumeButton.gameObject : null);
        }

        private static void ClearSelection()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (HelpPanelUI.IsOpen || EventModalUI.HasVisibleModal) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_open) Close();
                else Open();
            }
        }

        private void RefreshScore()
        {
            var gm = GameManager.Instance;
            var p  = gm?.GetCurrentPlayer();
            if (p == null) return;

            if (_statusTitleText != null)
                _statusTitleText.text = "ステータス詳細";

            if (_scoreBreakdownText != null)
            {
                var all = gm.GetAllPlayers();
                _scoreBreakdownText.text = ScoreCalculator.BuildPauseStatusReport(p, all);
                _scoreBreakdownText.text +=
                    $"\n\n■ {p.Character.SkillName()}（ワザ）\n{p.Character.SkillDescription()}";

                int sThreshold = DifficultyRules.RankSThreshold(GameSession.Difficulty);
                _scoreBreakdownText.text +=
                    $"\n\n【ランク目安】S: {sThreshold}pt以上 / A: 500pt以上（IF15以上なら400pt）/ B: 300pt以上 / C: それ未満";
            }
        }

        /// <summary>誤操作で進行中のゲームが消えないよう、タイトルへ戻るは2段階確認（押し直し）。</summary>
        private void GoToTitle()
        {
            if (!_titleConfirmArmed)
            {
                ArmTitleConfirm();
                return;
            }

            Time.timeScale = 1f;
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
            SceneManager.LoadScene("TitleScene");
        }

        private void ArmTitleConfirm()
        {
            _titleConfirmArmed = true;
            var label = _titleButton != null ? _titleButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (label != null)
            {
                _titleButtonOriginalLabel ??= label.text;
                label.text = "本当に戻る？ もう一度押すと確定";
            }
            if (_titleConfirmResetCo != null) StopCoroutine(_titleConfirmResetCo);
            _titleConfirmResetCo = StartCoroutine(ResetTitleConfirmAfterDelay());
        }

        private void DisarmTitleConfirm()
        {
            _titleConfirmArmed = false;
            if (_titleConfirmResetCo != null)
            {
                StopCoroutine(_titleConfirmResetCo);
                _titleConfirmResetCo = null;
            }
            if (_titleButtonOriginalLabel != null)
            {
                var label = _titleButton != null ? _titleButton.GetComponentInChildren<TextMeshProUGUI>() : null;
                if (label != null) label.text = _titleButtonOriginalLabel;
            }
        }

        private System.Collections.IEnumerator ResetTitleConfirmAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3.5f);
            DisarmTitleConfirm();
            _titleConfirmResetCo = null;
        }

        private void ApplyChrome()
        {
            if (_panel != null)
            {
                GameUiChrome.ApplySurface(_panel.transform, new Color(0.12f, 0.14f, 0.20f, 0.98f));
                GameUiChrome.ApplyAccentRail(_panel.transform, new Color(0.86f, 0.68f, 0.28f, 0.88f), 6f);
            }

            if (_statusTitleText != null)
                GameUiChrome.ApplyReadable(_statusTitleText, new Color(1f, 0.94f, 0.70f, 1f), FontStyles.Bold);
            if (_scoreBreakdownText != null)
            {
                GameUiChrome.ApplyReadable(_scoreBreakdownText, GameUiChrome.MutedText);
                _scoreBreakdownText.enableAutoSizing = true;
                _scoreBreakdownText.fontSizeMin = HudTextStyle.Scale(13f);
                _scoreBreakdownText.fontSizeMax = HudTextStyle.Scale(18f);
                _scoreBreakdownText.textWrappingMode = TextWrappingModes.Normal;
            }
            if (_resumeButton != null) GameUiChrome.ApplyButton(_resumeButton, primary: true);
            if (_titleButton != null) GameUiChrome.ApplyButton(_titleButton, primary: false);
            if (_helpButton != null) GameUiChrome.ApplyButton(_helpButton, primary: false);
        }

        private Button CreateHelpButton()
        {
            if (_panel == null) return null;
            var existing = _panel.transform.Find("HelpButton");
            if (existing != null) return existing.GetComponent<Button>();
            var go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "遊び方";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            JapaneseFontProvider.Apply(tmp);
            return go.GetComponent<Button>();
        }

        private void ApplySafeLayout()
        {
            if (StaticUiAuthoring.IsBaked(transform)) return;
            if (_panel is not { } panel || panel.transform is not RectTransform panelRt) return;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(920f, 760f);
            EnsureBreakdownScroll();
            PlaceFooterButton(_titleButton, -260f, 240f);
            PlaceFooterButton(_helpButton, 0f, 240f);
            PlaceFooterButton(_resumeButton, 260f, 240f);
        }

        /// <summary>スコア内訳をスクロール可能なビューポートにラップする（長文の末尾欠落防止）。</summary>
        private void EnsureBreakdownScroll()
        {
            if (_panel == null || _scoreBreakdownText == null) return;
            if (_panel.transform.Find("PauseBreakdownScroll") != null) return;

            var scrollGo = new GameObject("PauseBreakdownScroll",
                typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(24f, 96f);
            scrollRt.offsetMax = new Vector2(-24f, -64f);
            var viewportImg = scrollGo.GetComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0f);
            viewportImg.raycastTarget = true;

            var bodyRt = _scoreBreakdownText.rectTransform;
            bodyRt.SetParent(scrollRt, false);
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = Vector2.zero;
            bodyRt.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = bodyRt;
            scroll.viewport = scrollRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            _scoreBreakdownText.overflowMode = TextOverflowModes.Overflow;
            _scoreBreakdownText.raycastTarget = false;
            UiSafeLayout.CreateScrollbar(scroll, scrollRt, new Color(0.86f, 0.68f, 0.28f, 0.55f));
        }

        private static void PlaceFooterButton(Button button, float x, float width)
        {
            if (button == null) return;
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, UiSafeLayout.OuterMargin);
            rt.sizeDelta = new Vector2(width, 56f);
        }
    }
}
