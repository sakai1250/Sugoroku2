using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §6.1 — リソースHUDの発光・パルス（増減色分け）。</summary>
    public class HudStatFlash : MonoBehaviour
    {
        public static readonly Color DecreaseGlow = new(1f, 0.2f, 0.2f);      // #FF3333
        public static readonly Color IncreaseGlow = new(0.2f, 1f, 0.4f);    // #33FF66

        [SerializeField] private float flashDuration = 0.5f;

        private readonly Dictionary<TextMeshProUGUI, Coroutine> _textRoutines = new();
        private readonly Dictionary<Transform, Coroutine> _iconRoutines = new();

        private TextMeshProUGUI _money;
        private TextMeshProUGUI _ifScore;
        private TextMeshProUGUI _mental;
        private TextMeshProUGUI _virtue;
        private Slider _mentalSlider;
        private Transform _moneyIcon;
        private Transform _ifIcon;
        private Transform _mentalIcon;
        private Transform _virtueIcon;

        public void Bind(TextMeshProUGUI money, TextMeshProUGUI ifScore, TextMeshProUGUI mental,
            TextMeshProUGUI virtue, Slider mentalSlider)
        {
            _money        = money;
            _ifScore      = ifScore;
            _mental       = mental;
            _virtue       = virtue;
            _mentalSlider = mentalSlider;
            BindIconsFromBar();
        }

        private void BindIconsFromBar()
        {
            var bar = _money?.transform.parent;
            while (bar != null && bar.name != "ResourceBar" && bar.parent != null)
                bar = bar.parent;
            if (bar == null)
                bar = GameObject.Find("ResourceBar")?.transform;
            if (bar == null) return;

            _moneyIcon  = FindIcon(bar, "MoneyIcon");
            _ifIcon     = FindIcon(bar, "IfIcon");
            _mentalIcon = FindIcon(bar, "MentalIcon");
            _virtueIcon = FindIcon(bar, "VirtueIcon");
        }

        private static Transform FindIcon(Transform bar, string name)
        {
            var t = bar.Find(name);
            if (t != null) return t;
            foreach (Transform child in bar)
            {
                t = child.Find(name);
                if (t != null) return t;
            }
            return null;
        }

        public void Flash(int money, int ifScore, int mental, int virtue)
        {
            if (money   != 0) FlashStat(_money, _moneyIcon, money   > 0);
            if (ifScore != 0) FlashStat(_ifScore, _ifIcon, ifScore > 0);
            if (mental  != 0) FlashStat(_mental, _mentalIcon, mental  > 0);
            if (virtue  != 0) FlashStat(_virtue, _virtueIcon, virtue  > 0);
            if (mental != 0 && _mentalSlider != null)
                StartCoroutine(FlashSlider(mental > 0));
        }

        private void FlashStat(TextMeshProUGUI tmp, Transform icon, bool positive)
        {
            if (tmp != null) FlashText(tmp, positive);
            if (icon != null) FlashIcon(icon, positive);
        }

        private void FlashText(TextMeshProUGUI tmp, bool positive)
        {
            if (_textRoutines.TryGetValue(tmp, out var c) && c != null)
                StopCoroutine(c);
            _textRoutines[tmp] = StartCoroutine(FlashTextCoroutine(tmp, positive));
        }

        private void FlashIcon(Transform icon, bool positive)
        {
            if (_iconRoutines.TryGetValue(icon, out var c) && c != null)
                StopCoroutine(c);
            _iconRoutines[icon] = StartCoroutine(FlashIconCoroutine(icon, positive));
        }

        private IEnumerator FlashTextCoroutine(TextMeshProUGUI tmp, bool positive)
        {
            var glow    = positive ? IncreaseGlow : DecreaseGlow;
            var baseCol = Color.white;
            var rt      = tmp.rectTransform;
            var baseScale = rt.localScale;
            var basePos   = rt.anchoredPosition;

            float flashIn = GameConfig.AnimationDuration(0.1f);
            float elapsed = 0f;
            while (elapsed < flashIn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashIn;
                tmp.color = Color.Lerp(baseCol, glow, t);
                tmp.fontStyle = FontStyles.Bold;
                yield return null;
            }

            tmp.color = glow;
            elapsed = 0f;
            float pulseDur = GameConfig.AnimationDuration(flashDuration) - flashIn;
            while (elapsed < pulseDur)
            {
                elapsed += Time.deltaTime;
                float t = JuiceMath.EaseOutQuad(elapsed / pulseDur);

                if (positive)
                {
                    float scale = Mathf.Lerp(1f, 1.08f, Mathf.Sin(t * Mathf.PI));
                    rt.localScale = baseScale * scale;
                    rt.anchoredPosition = basePos + Vector2.up * (8f * Mathf.Sin(t * Mathf.PI));
                }
                else
                {
                    float scale = 1f - 0.12f * Mathf.Sin(t * Mathf.PI);
                    rt.localScale = baseScale * scale;
                }
                yield return null;
            }

            tmp.color = baseCol;
            tmp.fontStyle = FontStyles.Normal;
            rt.localScale = baseScale;
            rt.anchoredPosition = basePos;
            _textRoutines.Remove(tmp);
        }

        private IEnumerator FlashIconCoroutine(Transform icon, bool positive)
        {
            var img = icon.GetComponent<Image>();
            if (img == null) yield break;

            var baseCol = img.color;
            var glow    = positive ? IncreaseGlow : DecreaseGlow;
            var rt      = icon as RectTransform;
            var baseScale = icon.localScale;
            var basePos   = rt != null ? rt.anchoredPosition : Vector2.zero;

            float elapsed = 0f;
            float scaledDuration = GameConfig.AnimationDuration(flashDuration);
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaledDuration;
                float pulse = Mathf.Sin(t * Mathf.PI);
                img.color = Color.Lerp(baseCol, glow, pulse);

                if (rt != null)
                {
                    if (positive)
                    {
                        icon.localScale = baseScale * (1f + 0.1f * pulse);
                        rt.anchoredPosition = basePos + Vector2.up * (4f * pulse);
                    }
                    else
                        icon.localScale = baseScale * (1f - 0.1f * pulse);
                }
                yield return null;
            }

            img.color = baseCol;
            icon.localScale = baseScale;
            if (rt != null) rt.anchoredPosition = basePos;
            _iconRoutines.Remove(icon);
        }

        private IEnumerator FlashSlider(bool positive)
        {
            if (_mentalSlider?.fillRect == null) yield break;
            var img = _mentalSlider.fillRect.GetComponent<Image>();
            if (img == null) yield break;

            var baseCol = img.color;
            var glow    = positive ? IncreaseGlow : DecreaseGlow;
            float elapsed = 0f;
            float scaledDuration = GameConfig.AnimationDuration(flashDuration);
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.Sin((elapsed / scaledDuration) * Mathf.PI);
                img.color = Color.Lerp(baseCol, glow, pulse);
                yield return null;
            }
            img.color = baseCol;
        }
    }
}
