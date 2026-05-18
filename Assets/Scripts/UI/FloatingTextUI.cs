using System.Collections;
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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        }

        public void Show(string text, Vector3 worldPos, Color color) =>
            StartCoroutine(FloatCoroutine(text, worldPos, color));

        public void ShowStatChange(int money, int ifScore, int mental, int virtue, Vector3 worldPos)
        {
            if (money != 0)
            {
                string sign = money > 0 ? "+" : "";
                Show($"所持金 {sign}{money}万", worldPos + Vector3.up * 0.35f,
                    money > 0 ? FloatGreen : FloatRed);
            }
            if (ifScore != 0)
            {
                string sign = ifScore > 0 ? "+" : "";
                Show($"IF {sign}{ifScore} pt", worldPos + Vector3.up * 0.65f,
                    ifScore > 0 ? FloatBlue : FloatRed);
            }
            if (mental != 0)
            {
                string sign = mental > 0 ? "+" : "";
                Show($"メンタル {sign}{mental}", worldPos + Vector3.up * 0.95f,
                    mental > 0 ? FloatGreen : FloatRed);
            }
            if (virtue != 0)
            {
                string sign = virtue > 0 ? "+" : "";
                Show($"徳 {sign}{virtue}", worldPos + Vector3.up * 1.25f,
                    virtue > 0 ? FloatGreen : FloatRed);
            }
        }

        private IEnumerator FloatCoroutine(string text, Vector3 worldPos, Color color)
        {
            var go = new GameObject("FloatingText");
            if (_canvas != null) go.transform.SetParent(_canvas.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.color     = color;
            tmp.fontSize  = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 44f);

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

            float dur = GameConfig.FloatingTextDuration;
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
