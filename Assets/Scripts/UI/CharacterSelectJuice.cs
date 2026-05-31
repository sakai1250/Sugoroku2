using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Audio;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §2.4 — キャラ選択の Juice（選択跳ね・決定暗転・ネオン波紋）。</summary>
    public class CharacterSelectJuice : MonoBehaviour
    {
        [SerializeField] private float jumpHeight   = 18f;
        [SerializeField] private float jumpDuration = 0.22f;
        [SerializeField] private float confirmFade  = 0.22f;
        [SerializeField] private float confirmHold  = 0.35f;

        private Canvas     _canvas;
        private Image      _fadeOverlay;
        private RectTransform _rippleParent;
        private bool       _confirming;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            EnsureAudio();
            BuildOverlay();
        }

        private static void EnsureAudio()
        {
            if (GameAudioController.Instance != null) return;
            var go = new GameObject("GameAudio");
            go.AddComponent<GameAudioController>();
        }

        private void BuildOverlay()
        {
            if (_canvas == null) return;

            var fadeGo = new GameObject("ConfirmFadeOverlay", typeof(RectTransform));
            fadeGo.transform.SetParent(_canvas.transform, false);
            fadeGo.transform.SetAsLastSibling();
            _fadeOverlay = fadeGo.AddComponent<Image>();
            _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _fadeOverlay.raycastTarget = false;
            var fadeRt = fadeGo.GetComponent<RectTransform>();
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
            fadeGo.SetActive(false);

            var rippleGo = new GameObject("RippleParent", typeof(RectTransform));
            rippleGo.transform.SetParent(_canvas.transform, false);
            rippleGo.transform.SetAsLastSibling();
            _rippleParent = rippleGo.GetComponent<RectTransform>();
            _rippleParent.anchorMin = _rippleParent.anchorMax = new Vector2(0.5f, 0.5f);
            _rippleParent.sizeDelta = Vector2.zero;
        }

        public void PlaySelectionChanged(Transform cardTransform, bool isNewSelection)
        {
            if (cardTransform == null) return;
            if (isNewSelection)
                GameAudioController.Instance?.PlayRetroSelect();
            StartCoroutine(CardJumpCoroutine(cardTransform as RectTransform));
        }

        public void PlayConfirmSequence(RectTransform cardRect, CharacterType character, System.Action onComplete)
        {
            if (_confirming)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(ConfirmSequenceCoroutine(cardRect, character, onComplete));
        }

        private IEnumerator CardJumpCoroutine(RectTransform card)
        {
            if (card == null) yield break;

            Vector2 basePos = card.anchoredPosition;
            float duration = GameConfig.AnimationDuration(jumpDuration);
            float half = duration * 0.45f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float y = Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
                card.anchoredPosition = basePos + Vector2.up * y;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration - half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration - half);
                float y = (1f - Mathf.Sin(t * Mathf.PI * 0.5f)) * jumpHeight;
                card.anchoredPosition = basePos + Vector2.up * y;
                yield return null;
            }

            card.anchoredPosition = basePos;
        }

        private IEnumerator ConfirmSequenceCoroutine(RectTransform cardRect, CharacterType character, System.Action onComplete)
        {
            _confirming = true;
            try
            {
                GameAudioController.Instance?.PlayDoorClose();

                if (_fadeOverlay != null)
                {
                    _fadeOverlay.gameObject.SetActive(true);
                    float elapsed = 0f;
                    float fadeDuration = GameConfig.AnimationDuration(confirmFade);
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float a = Mathf.Lerp(0f, 0.72f, elapsed / fadeDuration);
                        _fadeOverlay.color = new Color(0f, 0f, 0.02f, a);
                        yield return null;
                    }
                }

                if (cardRect != null && _rippleParent != null)
                {
                    Vector2 localPos = WorldToCanvasLocal(cardRect);
                    yield return SpawnNeonRipples(localPos, character.AccentColor());
                }

                float hold = 0f;
                float holdDuration = GameConfig.AnimationDuration(confirmHold);
                while (hold < holdDuration)
                {
                    hold += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                _confirming = false;
                onComplete?.Invoke();
            }
        }

        private Vector2 WorldToCanvasLocal(RectTransform target)
        {
            if (_canvas == null || target == null) return Vector2.zero;
            var canvasRt = _canvas.GetComponent<RectTransform>();
            Vector3 world = target.TransformPoint(target.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera ?? Camera.main, world),
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var local);
            return local;
        }

        private IEnumerator SpawnNeonRipples(Vector2 localCenter, Color accent)
        {
            if (_rippleParent == null) yield break;

            Color c1 = new Color(accent.r, accent.g, accent.b, 0.85f);
            Color c2 = new Color(1f, 0.25f, 0.75f, 0.65f);
            Color c3 = new Color(0.3f, 0.95f, 1f, 0.5f);

            yield return AnimateRipple(localCenter, c1, 0f, 420f, GameConfig.AnimationDuration(0.35f));
            yield return AnimateRipple(localCenter, c2, 0f, 520f, GameConfig.AnimationDuration(0.35f));
            yield return AnimateRipple(localCenter, c3, 0f, 620f, GameConfig.AnimationDuration(0.35f));
        }

        private IEnumerator AnimateRipple(Vector2 localCenter, Color color, float delay, float maxSize, float duration)
        {
            if (delay > 0f) yield return new WaitForSeconds(GameConfig.AnimationDuration(delay));

            var go = new GameObject("NeonRipple", typeof(RectTransform));
            go.transform.SetParent(_rippleParent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = CreateRingSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = localCenter;
            rt.sizeDelta = new Vector2(40f, 40f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.15f, maxSize / 40f, EaseOutQuad(t));
                rt.localScale = Vector3.one * scale;
                float alpha = color.a * (1f - t);
                img.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(go);
        }

        private static Sprite _ringSprite;

        private static Sprite CreateRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            float outer = center - 1f;
            float inner = center * 0.62f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = d >= inner && d <= outer ? 1f : 0f;
                if (d > inner - 1f && d < inner + 1f) a = Mathf.Clamp01(d - (inner - 1f));
                if (d > outer - 1f && d < outer + 1f) a = Mathf.Min(a, Mathf.Clamp01((outer + 1f) - d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _ringSprite;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }
}
