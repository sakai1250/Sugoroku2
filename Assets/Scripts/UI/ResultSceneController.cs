using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    public class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button          _titleButton;

        private void Start()
        {
            _titleText   ??= FindTmp("ResultTitle");
            _bodyText    ??= FindTmp("ResultBody");
            _titleButton ??= FindBtn("TitleButton");
            if (_titleButton != null) _titleButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
            ShowResults();
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
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("【ランク別進路】");
            lines.AppendLine("S: 大手メーカー研究職  |  A: 博士進学  |  B: 修士就職  |  C: ポスドク候補");
            lines.AppendLine();

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                lines.AppendLine(GraduationOutcomeResolver.BuildPlayerReport(p, i));
                if (hero == null && p.Status == PlayerStatus.Graduated && !p.IsCpu)
                    hero = GraduationOutcomeResolver.Resolve(p);
            }

            if (hero == null)
            {
                foreach (var p in players)
                {
                    if (p.Status != PlayerStatus.Graduated) continue;
                    hero = GraduationOutcomeResolver.Resolve(p);
                    break;
                }
            }

            if (hero != null)
            {
                lines.AppendLine("────────────────");
                lines.AppendLine($"あなたの進路: ランク {hero.Value.Rank} → {hero.Value.CareerPath}");
                lines.AppendLine($"（{hero.Value.Subtitle}）");
            }

            if (_bodyText != null) _bodyText.text = lines.ToString().TrimEnd();
            EndSceneVisuals.ApplyGraduation(transform, hero);
        }

        private TextMeshProUGUI FindTmp(string n) => transform.Find(n)?.GetComponent<TextMeshProUGUI>();
        private Button FindBtn(string n) => transform.Find(n)?.GetComponent<Button>();
    }
}
