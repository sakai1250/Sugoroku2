using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Game;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    public class PlayerDetailUI : MonoBehaviour
    {
        [SerializeField] private GameObject      _panel;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _characterText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button          _closeButton;

        private void Awake() => BindReferences();

        private void Start()
        {
            BindReferences();
            if (_panel       != null) _panel.SetActive(false);
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        }

        private void BindReferences()
        {
            _panel ??= gameObject.name == "PlayerDetailPanel" ? gameObject : UiBindingUtility.FindObject("PlayerDetailPanel");
            _nameText ??= transform.Find("DetailNameText")?.GetComponent<TextMeshProUGUI>();
            _characterText ??= transform.Find("DetailCharacterText")?.GetComponent<TextMeshProUGUI>();
            _statusText ??= transform.Find("DetailStatusText")?.GetComponent<TextMeshProUGUI>();
            _scoreText ??= transform.Find("DetailScoreText")?.GetComponent<TextMeshProUGUI>();
            _closeButton ??= transform.Find("CloseButton")?.GetComponent<Button>();
            if (_panel != null && _closeButton != null)
                UiSafeLayout.LayoutCloseButton(_panel.transform, _closeButton.transform);
        }

        public void Show(PlayerData player)
        {
            if (_panel != null) _panel.SetActive(true);

            if (_nameText      != null)
            {
                _nameText.text  = PlayerIdentity.FormatHudLabel(player);
                _nameText.color = player.PieceTint;
            }
            if (_characterText != null) _characterText.text = $"{player.Character.DisplayName()}\nワザ: {player.Character.SkillName()}\n{player.Character.SkillDescription()}";

            string statusIcon = player.Status switch
            {
                PlayerStatus.Graduated => "[修了]",
                PlayerStatus.Dropout   => "[脱落]",
                _                      => "[進行中]"
            };

            if (_statusText != null)
                _statusText.text =
                    $"{statusIcon}\n" +
                    $"所持金: {player.Money}万\n" +
                    $"IF: {player.IfScore}\n" +
                    $"メンタル: {player.Mental}/{player.MaxMental}\n" +
                    $"徳: {player.Virtue}\n" +
                    $"位置: {player.BoardPosition}マス目\n" +
                    $"休み: {player.SkipTurns}ターン\n" +
                    $"回避: {player.IgnoreNextEvents}";

            if (_scoreText != null)
                _scoreText.text = $"計算 {player.CalculateScore()}";
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
