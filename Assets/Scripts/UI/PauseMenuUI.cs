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
    }
}
