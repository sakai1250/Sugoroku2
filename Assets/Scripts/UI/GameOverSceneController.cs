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
            if (_titleButton != null) _titleButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
            ApplyReason(GameSession.LastGameOverReason);
        }

        private void ApplyReason(GameOverReason reason)
        {
            var outcome = GameOverOutcomeResolver.Resolve(reason);

            if (_titleText != null)
            {
                _titleText.text = outcome.Title;
                JapaneseFontProvider.Apply(_titleText);
            }
            if (_bodyText != null)
            {
                _bodyText.text = outcome.Body;
                JapaneseFontProvider.Apply(_bodyText);
                LayoutBodyForVisual(outcome.VisualStyle);
            }

            EndSceneVisuals.ApplyGameOver(transform, outcome);
            if (_bodyText != null)
                LayoutBodyForVisual(outcome.VisualStyle);
            _juice?.Play(outcome);
        }

        private void LayoutBodyForVisual(GameOverVisualStyle style)
        {
            if (_bodyText == null) return;
            var rt = _bodyText.rectTransform;

            if (style == GameOverVisualStyle.ExpulsionList)
            {
                rt.anchoredPosition = new Vector2(0f, -290f);
                rt.sizeDelta        = new Vector2(780f, 130f);
                _bodyText.color     = new Color(0.82f, 0.82f, 0.88f);
            }
            else
            {
                rt.anchoredPosition = new Vector2(0f, -60f);
                rt.sizeDelta        = new Vector2(800f, 280f);
            }
        }

        private TextMeshProUGUI FindTmp(string n) => transform.Find(n)?.GetComponent<TextMeshProUGUI>();
        private Button FindBtn(string n) => transform.Find(n)?.GetComponent<Button>();
    }
}
