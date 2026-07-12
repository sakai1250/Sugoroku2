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

        private bool _open;

        private void Start()
        {
            _panel ??= gameObject.name == "StatusPanel" ? gameObject : UiBindingUtility.FindObject("StatusPanel");
            _statusTitleText ??= transform.Find("StatusTitle")?.GetComponent<TextMeshProUGUI>();
            _scoreBreakdownText ??= transform.Find("ScoreBreakdownText")?.GetComponent<TextMeshProUGUI>();
            _resumeButton ??= transform.Find("ResumeButton")?.GetComponent<Button>();
            _titleButton ??= transform.Find("TitleButton")?.GetComponent<Button>();
            ApplySafeLayout();
            ApplyChrome();
            if (_panel != null) _panel.SetActive(false);
            _resumeButton?.onClick.AddListener(Close);
            _titleButton?.onClick.AddListener(GoToTitle);
        }

        public void Open()
        {
            _open = true;
            Time.timeScale = 0f;
            if (_panel != null) _panel.SetActive(true);
            RefreshScore();
        }

        public void Close()
        {
            _open = false;
            Time.timeScale = 1f;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Update()
        {
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
            }
        }

        private void GoToTitle()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
            SceneManager.LoadScene("TitleScene");
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
                GameUiChrome.ApplyReadable(_scoreBreakdownText, GameUiChrome.MutedText);
            if (_resumeButton != null) GameUiChrome.ApplyButton(_resumeButton, primary: true);
            if (_titleButton != null) GameUiChrome.ApplyButton(_titleButton, primary: false);
        }

        private void ApplySafeLayout()
        {
            if (_panel is not { } panel || panel.transform is not RectTransform panelRt) return;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(920f, 760f);
            PlaceFooterButton(_titleButton, -170f);
            PlaceFooterButton(_resumeButton, 170f);
        }

        private static void PlaceFooterButton(Button button, float x)
        {
            if (button == null) return;
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, UiSafeLayout.OuterMargin);
            rt.sizeDelta = new Vector2(300f, 56f);
        }
    }
}
