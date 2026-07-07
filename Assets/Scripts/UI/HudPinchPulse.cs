using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>HUDのピンチ表示を「最も危険な1セルの明滅」に集約する。</summary>
    public class HudPinchPulse : MonoBehaviour
    {
        private Transform _target;
        private Image _targetImage;
        private Color _baseColor;
        private Coroutine _pulseRoutine;

        public void SetTarget(Transform target)
        {
            if (_target == target) return;

            RestoreCurrent();
            _target = target;
            _targetImage = _target != null ? _target.GetComponent<Image>() : null;
            if (_targetImage == null) return;

            _baseColor = _targetImage.color;
            _pulseRoutine = StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            var danger = new Color(1f, 0.20f, 0.20f, 0.95f);
            while (_targetImage != null)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 5.8f) + 1f) * 0.5f;
                _targetImage.color = Color.Lerp(_baseColor, danger, t * 0.55f);
                yield return null;
            }
        }

        private void OnDisable() => RestoreCurrent();
        private void OnDestroy() => RestoreCurrent();

        private void RestoreCurrent()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }
            if (_targetImage != null)
                _targetImage.color = _baseColor;
            _target = null;
            _targetImage = null;
        }
    }
}
