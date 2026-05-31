using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Game;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §6.3 — イベント時に盤面側を暗く／ボカし風に見せる。</summary>
    public class GameWorldPresentationDimmer : MonoBehaviour
    {
        public static GameWorldPresentationDimmer Instance { get; private set; }

        [SerializeField] private Image _overlay;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private float _eventAlpha   = 0.40f;

        private Coroutine _fadeRoutine;
        private bool      _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            EnsureOverlay();
            SetAlpha(0f, instant: true);
        }

        private void OnEnable() => TrySubscribe();
        private void OnDisable() => Unsubscribe();

        private void TrySubscribe()
        {
            if (_subscribed || TurnManager.Instance == null) return;
            TurnManager.Instance.OnStateChanged += HandleTurnState;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || TurnManager.Instance == null) return;
            TurnManager.Instance.OnStateChanged -= HandleTurnState;
            _subscribed = false;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var go = new GameObject("WorldDimOverlay", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            _overlay = go.AddComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public void ShowForEvent() => FadeTo(_eventAlpha);
        public void Hide() => FadeTo(0f);

        private void HandleTurnState(TurnState state)
        {
            if (state == TurnState.Event || state == TurnState.Apply)
                ShowForEvent();
            else if (state == TurnState.WaitAction || state == TurnState.Moving)
                Hide();
        }

        private void FadeTo(float targetAlpha)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeCoroutine(targetAlpha));
        }

        private System.Collections.IEnumerator FadeCoroutine(float targetAlpha)
        {
            EnsureOverlay();
            float start = _overlay.color.a;
            float elapsed = 0f;
            float duration = GameConfig.AnimationDuration(_fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = JuiceMath.EaseOutQuad(elapsed / duration);
                SetAlpha(Mathf.Lerp(start, targetAlpha, t));
                yield return null;
            }
            SetAlpha(targetAlpha);
            _fadeRoutine = null;
        }

        private void SetAlpha(float a, bool instant = false)
        {
            if (_overlay == null) return;
            var c = _overlay.color;
            c.a = a;
            _overlay.color = c;
            // 暗転オーバーレイがイベントモーダルのクリックを奪わないよう常に無効
            _overlay.raycastTarget = false;
        }
    }
}
