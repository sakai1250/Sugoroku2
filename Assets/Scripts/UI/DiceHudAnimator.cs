using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Game;
using Sugoroku.Visual;

namespace Sugoroku.UI
{
    /// <summary>HUD の DiceIcon / DiceResult をサイコロ演出と同期する。</summary>
    [DisallowMultipleComponent]
    public class DiceHudAnimator : MonoBehaviour
    {
        [SerializeField] private Image           _diceImage;
        [SerializeField] private RectTransform     _diceRect;
        [SerializeField] private TextMeshProUGUI   _resultText;

        private Vector3 _baseScale;
        private Vector2 _basePos;
        private bool    _rolling;

        private void Awake()
        {
            _diceImage ??= GetComponent<Image>();
            _diceRect  ??= GetComponent<RectTransform>();
            if (_diceRect != null)
            {
                _baseScale = _diceRect.localScale;
                _basePos   = _diceRect.anchoredPosition;
            }
        }

        public void Bind(Image image, TextMeshProUGUI resultText)
        {
            _diceImage  = image;
            _resultText = resultText;
            if (_diceImage != null)
                _diceRect = _diceImage.rectTransform;
            Awake();
        }

        public IEnumerator PlayRoll(int finalValue, System.Action<int> onFaceShown)
        {
            if (_diceImage == null) yield break;
            _rolling = true;
            yield return DiceJuice.PlayHudRoll(_diceImage, _diceRect, finalValue, onFaceShown);
            ResetTransform();
            _rolling = false;
        }

        public IEnumerator PlayResultPunch(int value)
        {
            if (_resultText != null)
                _resultText.text = $"サイコロ: {value}";

            if (_diceRect != null)
                yield return DiceJuice.PunchScale(_diceRect, 1.25f, 0.24f);
            if (_resultText != null)
                yield return DiceJuice.PunchScale(_resultText.rectTransform, 1.1f, 0.18f);
        }

        private void ResetTransform()
        {
            if (_diceRect == null) return;
            _diceRect.localScale        = _baseScale;
            _diceRect.anchoredPosition  = _basePos;
            _diceRect.localEulerAngles  = Vector3.zero;
            if (_diceImage != null) _diceImage.color = Color.white;
        }

        public bool IsRolling => _rolling;
    }
}
