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
            // 動きを減らす設定では明滅せず、危険色の静止表示のみ行う。
            if (Sugoroku.Data.GameSession.ReduceMotion)
            {
                if (_targetImage != null) _targetImage.color = Color.Lerp(_baseColor, danger, 0.55f);
                yield break;
            }

            float startTime = Time.unscaledTime;
            while (_targetImage != null)
            {
                // 明滅は最長5秒に制限し、以降は危険色で静止する（WCAG 2.3.3: アニメーション5秒以下）。
                float elapsed = Time.unscaledTime - startTime;
                float f = elapsed < 5f
                    ? (Mathf.Sin(Time.unscaledTime * 5.8f) + 1f) * 0.5f
                    : 1f;
                _targetImage.color = Color.Lerp(_baseColor, danger, f * 0.55f);
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
