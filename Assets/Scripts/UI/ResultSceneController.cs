using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Data;
using Sugoroku.Audio;

namespace Sugoroku.UI
{
    public class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button          _titleButton;

        private GraduationOutcome? _heroOutcome;
        private PlayerSnapshot     _heroPlayer;
        private bool               _hasDailyScore;
        private int                _dailyScore;
        private int                _dailyBestScore;

        private void Start()
        {
            GameAudioController.Instance?.PlayResultBgm();
            _titleText   ??= FindTmp("ResultTitle");
            _bodyText    ??= FindTmp("ResultBody");
            _titleButton ??= FindBtn("TitleButton");
            ApplyChrome();
            if (_titleButton != null) _titleButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
            ShowResults();
            EnsureShareButtons();
            EnsureStatHistoryChart();
        }

        private void ShowResults()
        {
            if (_titleText != null) _titleText.text = "修了発表 — 進路発表";

            var players = (GameSession.LastPlayers ?? System.Array.Empty<PlayerSnapshot>())
                .OrderByDescending(p => p.Status == PlayerStatus.Graduated ? 1 : 0)
                .ThenBy(p => p.FinishRank > 0 ? p.FinishRank : int.MaxValue)
                .ThenByDescending(p => p.CalculateScore())
                .ToArray();

            GraduationOutcome? hero = null;
            PlayerSnapshot heroPlayer = default;
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("【ランク別進路】");
            lines.AppendLine("S: 教授  |  A: 大手メーカー  |  B: 修士就職  |  C: 実家稼業");
            lines.AppendLine();

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                lines.AppendLine(GraduationOutcomeResolver.BuildPlayerReport(p, i));
                if (hero == null && p.Status == PlayerStatus.Graduated && !p.IsCpu)
                {
                    hero = GraduationOutcomeResolver.Resolve(p);
                    heroPlayer = p;
                }
            }

            if (hero == null)
            {
                foreach (var p in players)
                {
                    if (p.Status != PlayerStatus.Graduated) continue;
                    hero = GraduationOutcomeResolver.Resolve(p);
                    heroPlayer = p;
                    break;
                }
            }

            _heroOutcome = hero;
            _heroPlayer  = heroPlayer;

            if (hero != null)
            {
                lines.AppendLine("────────────────");
                lines.AppendLine($"あなたの進路: ランク {hero.Value.Rank} → {hero.Value.CareerPath}");
                lines.AppendLine($"（{hero.Value.Subtitle}）");
            }

            if (hero != null && !heroPlayer.IsCpu)
                AchievementEvaluator.OnGraduated(heroPlayer, hero.Value.Rank);

            if (GameSession.IsDailyChallenge)
            {
                AchievementEvaluator.OnDailyChallengePlayed();
                AppendDailyScore(lines, heroPlayer);
            }

            if (_bodyText != null) _bodyText.text = lines.ToString().TrimEnd();
            EndSceneVisuals.ApplyGraduation(transform, hero);
            ResultSceneLayout.Apply(transform);
        }

        private void AppendDailyScore(System.Text.StringBuilder lines, PlayerSnapshot heroPlayer)
        {
            int score = heroPlayer.CalculateScore();
            int best = AchievementStore.GetDailyBestScore(GameSession.DailySeed);
            bool isNewBest = score > best;
            if (isNewBest) AchievementStore.SetDailyBestScore(GameSession.DailySeed, score);
            int bestScore = isNewBest ? score : best;

            _hasDailyScore = true;
            _dailyScore = score;
            _dailyBestScore = bestScore;

            lines.AppendLine("────────────────");
            lines.AppendLine($"本日のスコア: {score:N0}点" + (isNewBest ? "（自己ベスト更新！）" : $"（自己ベスト: {bestScore:N0}点）"));
        }

        private TextMeshProUGUI FindTmp(string n) => transform.Find(n)?.GetComponent<TextMeshProUGUI>();
        private Button FindBtn(string n) => transform.Find(n)?.GetComponent<Button>();

        /// <summary>「診断メーカー」風の結果カード画像を保存/シェアするボタン。requirements.md §1.1 WebGL 想定。</summary>
        private void EnsureShareButtons()
        {
            if (_heroOutcome == null) return;

            var row = new GameObject("ShareButtonRow", typeof(RectTransform));
            row.transform.SetParent(transform, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0f);
            rowRt.anchorMax = new Vector2(0.5f, 0f);
            rowRt.pivot = new Vector2(0.5f, 0f);
            rowRt.sizeDelta = new Vector2(620f, 84f);
            rowRt.anchoredPosition = new Vector2(0f, 24f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateShareButton(row.transform, "画像を保存", SaveShareImage);
            CreateShareButton(row.transform, "Xでシェア", OpenShareIntent);
        }

        /// <summary>IF/メンタルの推移チャートを1枚追加する。履歴が2件未満なら何も表示しない。</summary>
        private void EnsureStatHistoryChart()
        {
            if (_heroPlayer.History == null || _heroPlayer.History.Count < 2) return;

            var existing = transform.Find("StatHistoryChart");
            if (existing != null) Destroy(existing.gameObject);

            var chartGo = new GameObject("StatHistoryChart", typeof(RectTransform), typeof(Image));
            chartGo.transform.SetParent(transform, false);
            var chartRt = (RectTransform)chartGo.transform;
            chartRt.anchorMin = new Vector2(1f, 0.5f);
            chartRt.anchorMax = new Vector2(1f, 0.5f);
            chartRt.pivot = new Vector2(1f, 0.5f);
            chartRt.anchoredPosition = new Vector2(-36f, -138f);
            chartRt.sizeDelta = new Vector2(350f, 160f);

            var bg = chartGo.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.15f, 0.92f);
            bg.raycastTarget = false;

            var labelGo = new GameObject("ChartLabel", typeof(RectTransform));
            labelGo.transform.SetParent(chartRt, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "IF・メンタルの推移";
            label.fontSize = 16f;
            label.color = new Color(0.85f, 0.87f, 0.92f);
            label.alignment = TextAlignmentOptions.TopLeft;
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -6f);
            labelRt.sizeDelta = new Vector2(-16f, 24f);

            var plotGo = new GameObject("PlotArea", typeof(RectTransform));
            plotGo.transform.SetParent(chartRt, false);
            var plotRt = (RectTransform)plotGo.transform;
            plotRt.anchorMin = Vector2.zero;
            plotRt.anchorMax = Vector2.one;
            plotRt.offsetMin = new Vector2(12f, 12f);
            plotRt.offsetMax = new Vector2(-12f, -32f);

            StatHistoryChartView.Draw(plotRt, _heroPlayer.History, _heroPlayer.MaxMental);
            ResultSceneLayout.BringReadableContentToFront(transform);
        }

        private Button CreateShareButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"ShareButton_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            var btn = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: false);
            btn.onClick.AddListener(onClick);
            return btn;
        }

        private void SaveShareImage()
        {
            if (_heroOutcome == null) return;

            var png = ShareImageBuilder.BuildPng(_heroPlayer, _heroOutcome.Value);
            string filename = $"sugoroku_result_{_heroOutcome.Value.Rank}.png";
            string savedPath = SharePlatformUtility.SaveImage(png, filename);

            if (_bodyText != null && !string.IsNullOrEmpty(savedPath))
                _bodyText.text += $"\n\n（画像を保存しました: {savedPath}）";
        }

        private void OpenShareIntent()
        {
            if (_heroOutcome == null) return;

            string text = $"研究者人生ランク「{_heroOutcome.Value.Rank}」→ {_heroOutcome.Value.CareerPath}";
            if (GameSession.IsDailyChallenge && _hasDailyScore)
                text += $"\n今日のスコア: {_dailyScore:N0}点（自己ベスト: {_dailyBestScore:N0}点）";
            text += "\n#すごろく研究者人生";
            SharePlatformUtility.OpenShareIntent(text);
        }

        private void ApplyChrome()
        {
            GameUiChrome.ApplySurface(transform, new Color(0.11f, 0.14f, 0.20f, 0.98f));
            GameUiChrome.ApplyAccentRail(transform, new Color(0.86f, 0.68f, 0.28f, 0.88f), 6f);

            if (_titleText != null)
            {
                GameUiChrome.ApplyReadable(_titleText, new Color(1f, 0.94f, 0.70f, 1f), FontStyles.Bold);
                HudTextStyle.ApplyOutlineSafe(_titleText, 0.16f, new Color(0f, 0f, 0f, 0.88f));
            }
            if (_bodyText != null)
            {
                _bodyText.color = new Color(0.90f, 0.93f, 0.98f);
                _bodyText.alignment = TextAlignmentOptions.TopLeft;
                HudTextStyle.ApplyOutlineSafe(_bodyText, 0.12f, new Color(0f, 0f, 0f, 0.85f));
            }
            if (_titleButton != null)
                GameUiChrome.ApplyButton(_titleButton, primary: true);
        }
    }
}
