using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Game;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    public class ResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject      _panel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform       _rankingParent;
        [SerializeField] private TextMeshProUGUI _rankingEntryPrefab;
        [SerializeField] private Button          _titleButton;

        private void Start()
        {
            AutoBind();
            StartCoroutine(SubscribeWhenReady());
            if (_panel       != null) _panel.SetActive(false);
            if (_titleButton != null) _titleButton.onClick.AddListener(GoToTitle);
        }

        private System.Collections.IEnumerator SubscribeWhenReady()
        {
            while (GameManager.Instance == null)
                yield return null;
            GameManager.Instance.OnAllFinished += ShowResult;
        }

        private void AutoBind()
        {
            _panel ??= gameObject.name == "ResultPanel" ? gameObject : UiBindingUtility.FindObject("ResultPanel");
            _titleText ??= transform.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>()
                           ?? UiBindingUtility.FindComponent<TextMeshProUGUI>("ResultTitle");
            _rankingParent ??= transform.Find("RankingParent")
                               ?? UiBindingUtility.FindTransform("RankingParent");
            _titleButton ??= transform.Find("TitleButton")?.GetComponent<Button>()
                             ?? FindResultTitleButton();
        }

        private Button FindResultTitleButton()
        {
            var panel = UiBindingUtility.FindObject("ResultPanel");
            return panel != null ? panel.transform.Find("TitleButton")?.GetComponent<Button>() : null;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnAllFinished -= ShowResult;
        }

        private void ShowResult()
        {
            if (_panel    != null) _panel.SetActive(true);
            if (_titleText != null) _titleText.text = "修了発表";

            var players = GameManager.Instance.GetAllPlayers()
                .OrderByDescending(p => p.Status == PlayerStatus.Graduated ? 1 : 0)
                .ThenBy(p => p.FinishRank > 0 ? p.FinishRank : int.MaxValue)
                .ThenByDescending(p => p.CalculateScore())
                .ToArray();

            if (_rankingParent != null)
            {
                foreach (Transform child in _rankingParent) Destroy(child.gameObject);

                for (int i = 0; i < players.Length; i++)
                {
                    var p     = players[i];
                    int score = p.CalculateScore();
                    string line;
                    if (p.Status == PlayerStatus.Graduated)
                    {
                        var outcome = GraduationOutcomeResolver.Resolve(PlayerSnapshot.From(p));
                        line = $"{i + 1}位  {p.Name}  [{p.Character.DisplayName()}]  ランク {outcome.Rank}\n" +
                               $"計算 {outcome.Score}  |  所持金 {p.Money}万 / IF {p.IfScore} / メンタル {p.Mental} / 徳 {p.Virtue}\n" +
                               $"→ {outcome.CareerPath}（{outcome.Subtitle}）";
                    }
                    else
                    {
                        line = $"{i + 1}位  {p.Name}  [{p.Character.DisplayName()}]\n→ 脱落";
                    }

                    if (_rankingEntryPrefab != null)
                    {
                        var entry = Instantiate(_rankingEntryPrefab, _rankingParent);
                        entry.text = line;
                    }
                    else
                    {
                        var go  = new GameObject($"Result_{i}");
                        go.transform.SetParent(_rankingParent, false);
                        var tmp = go.AddComponent<TextMeshProUGUI>();
                        tmp.text = line;
                        tmp.fontSize = HudTextStyle.Scale(14f);
                    }
                }
            }
        }

        private void GoToTitle()
        {
            Destroy(GameManager.Instance.gameObject);
            SceneManager.LoadScene("TitleScene");
        }
    }
}
