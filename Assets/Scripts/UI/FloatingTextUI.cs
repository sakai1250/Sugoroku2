using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §6.2 — 駒頭上のフローティングステータス表示。</summary>
    public class FloatingTextUI : MonoBehaviour
    {
        public static FloatingTextUI Instance { get; private set; }

        private static readonly Color FloatRed   = new(1f, 0.2f, 0.2f);
        private static readonly Color FloatBlue  = new(0.35f, 0.7f, 1f);
        private static readonly Color FloatGreen = new(0.2f, 1f, 0.4f);

        [SerializeField] private Canvas _canvas;

        private readonly Queue<FloatRequest> _queue = new();
        private Coroutine _queueRoutine;

        private readonly struct FloatRequest
        {
            public readonly string Text;
            public readonly Vector3 WorldPos;
            public readonly Color Color;

            public FloatRequest(string text, Vector3 worldPos, Color color)
            {
                Text = text;
                WorldPos = worldPos;
                Color = color;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        }

        public void Show(string text, Vector3 worldPos, Color color) =>
            Enqueue(new FloatRequest(text, worldPos, color));

        public void ShowStatChange(int money, int ifScore, int mental, int virtue, Vector3 worldPos)
        {
            var anchor = worldPos + Vector3.up * 0.65f;

            if (money != 0)
            {
                string sign = money > 0 ? "+" : "";
                Enqueue(new FloatRequest(
                    $"所持金 {sign}{money}万", anchor,
                    money > 0 ? FloatGreen : FloatRed));
            }

            if (ifScore != 0)
            {
                string sign = ifScore > 0 ? "+" : "";
                Enqueue(new FloatRequest(
                    $"IF {sign}{ifScore} pt", anchor,
                    ifScore > 0 ? FloatBlue : FloatRed));
            }

            if (mental != 0)
            {
                string sign = mental > 0 ? "+" : "";
                Enqueue(new FloatRequest(
                    $"メンタル {sign}{mental}", anchor,
                    mental > 0 ? FloatGreen : FloatRed));
            }

            if (virtue != 0)
            {
                string sign = virtue > 0 ? "+" : "";
                Enqueue(new FloatRequest(
                    $"徳 {sign}{virtue}", anchor,
                    virtue > 0 ? FloatGreen : FloatRed));
            }
        }

        private void Enqueue(FloatRequest request)
        {
            _queue.Enqueue(request);
            if (_queueRoutine == null)
                _queueRoutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            float gap = GameConfig.AnimationDuration(GameConfig.StatFloatGapSeconds);

            while (_queue.Count > 0)
            {
                var request = _queue.Dequeue();
                yield return FloatCoroutine(request.Text, request.WorldPos, request.Color);
                if (_queue.Count > 0 && gap > 0f)
                    yield return new WaitForSeconds(gap);
            }

            _queueRoutine = null;
        }

        private IEnumerator FloatCoroutine(string text, Vector3 worldPos, Color color)
        {
            var go = new GameObject("FloatingText");
            if (_canvas != null) go.transform.SetParent(_canvas.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.color     = color;
            tmp.fontSize  = HudTextStyle.JuiceFloatingFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            HudTextStyle.ApplyOutlineSafe(tmp, HudTextStyle.JuiceOutlineWidth, new Color(0f, 0f, 0f, 0.88f));

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 58f);

            var cam = Camera.main;
            if (cam != null && _canvas != null)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.GetComponent<RectTransform>(), screenPos,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                    out var localPos);
                rt.anchoredPosition = localPos;
            }

            float dur = GameConfig.AnimationDuration(GameConfig.FloatingTextDuration);
            Vector2 startPos = rt.anchoredPosition;
            const float rise = 55f;

            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float eased = JuiceMath.EaseOutQuad(t);
                tmp.color = JuiceMath.WithAlpha(color, 1f - t);
                rt.anchoredPosition = startPos + Vector2.up * (rise * eased);
                yield return null;
            }

            Destroy(go);
        }
    }
}
