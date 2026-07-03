using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    public class GameOverSceneController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Image           _accentImage;
        [SerializeField] private Button          _titleButton;
        [SerializeField] private GameOverSceneJuice _juice;

        private void Start()
        {
            _titleText   ??= FindTmp("GameOverTitle");
            _bodyText    ??= FindTmp("GameOverBody");
            _accentImage ??= transform.Find("AccentPanel")?.GetComponent<Image>();
            _titleButton ??= FindBtn("TitleButton");
            _juice       ??= GetComponent<GameOverSceneJuice>();
            if (_juice == null) _juice = gameObject.AddComponent<GameOverSceneJuice>();
            ApplyChrome();
            if (_titleButton != null) _titleButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
            ApplyReason(GameSession.LastGameOverReason);
        }

        private void ApplyReason(GameOverReason reason)
        {
            AchievementEvaluator.OnGameOver(reason);
            if (GameSession.IsDailyChallenge)
                AchievementEvaluator.OnDailyChallengePlayed();

            var outcome = GameOverOutcomeResolver.Resolve(reason);
            string dailyScoreLine = GameSession.IsDailyChallenge ? BuildDailyScoreLine() : "";

            if (_titleText != null)
            {
                _titleText.text = outcome.Title;
                JapaneseFontProvider.Apply(_titleText);
            }
            if (_bodyText != null)
            {
                _bodyText.text = string.IsNullOrEmpty(dailyScoreLine)
                    ? outcome.Body
                    : $"{outcome.Body}\n\n{dailyScoreLine}";
                JapaneseFontProvider.Apply(_bodyText);
            }

            EndSceneVisuals.ApplyGameOver(transform, outcome);
            GameOverLayout.ApplyContent(transform, outcome.VisualStyle);
            if (_accentImage != null)
            {
                var c = outcome.AccentColor;
                _accentImage.color = new Color(c.r, c.g, c.b, 0.32f);
                _accentImage.gameObject.SetActive(true);
            }
            _juice?.Play(outcome);
            EndSceneVisuals.BringGameOverContentToFront(transform);
        }

        private string BuildDailyScoreLine()
        {
            var players = GameSession.LastPlayers;
            if (players == null || players.Length == 0) return "";

            PlayerSnapshot player = players[0];
            for (int i = 0; i < players.Length; i++)
            {
                if (!players[i].IsCpu)
                {
                    player = players[i];
                    break;
                }
            }

            int score = player.CalculateScore();
            int best = AchievementStore.GetDailyBestScore(GameSession.DailySeed);
            bool isNewBest = score > best;
            if (isNewBest) AchievementStore.SetDailyBestScore(GameSession.DailySeed, score);
            int bestScore = isNewBest ? score : best;

            return $"本日のスコア: {score:N0}点" +
                   (isNewBest ? "（自己ベスト更新！）" : $"（自己ベスト: {bestScore:N0}点）");
        }

        private TextMeshProUGUI FindTmp(string n) => transform.Find(n)?.GetComponent<TextMeshProUGUI>();
        private Button FindBtn(string n) => transform.Find(n)?.GetComponent<Button>();

        private void ApplyChrome()
        {
            GameUiChrome.ApplySurface(transform, new Color(0.08f, 0.08f, 0.10f, 0.98f));
            GameUiChrome.ApplyAccentRail(transform, new Color(0.86f, 0.24f, 0.20f, 0.90f), 6f);
            if (_titleText != null)
                GameUiChrome.ApplyReadable(_titleText, new Color(1f, 0.78f, 0.70f, 1f), FontStyles.Bold);
            if (_bodyText != null)
                GameUiChrome.ApplyReadable(_bodyText, new Color(0.86f, 0.86f, 0.90f, 1f));
            if (_titleButton != null)
                GameUiChrome.ApplyButton(_titleButton, primary: false);
        }
    }
}
