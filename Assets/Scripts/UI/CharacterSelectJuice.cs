using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Audio;
using Sugoroku.Board;
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
            var cardRect = cardTransform as RectTransform;
            StartCoroutine(CardJumpCoroutine(cardRect));

            if (cardRect != null)
            {
                var accentImage = cardTransform.Find("CardTopRule")?.GetComponent<Image>();
                var accent = accentImage != null ? accentImage.color : new Color(0.86f, 0.68f, 0.28f, 1f);
                StartCoroutine(SelectionBurstCoroutine(cardRect, accent));
            }
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
            Vector3 baseScale = card.localScale;
            Quaternion baseRotation = card.localRotation;
            float duration = GameConfig.AnimationDuration(jumpDuration);
            float half = duration * 0.45f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float y = Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
                float punch = Mathf.Sin(t * Mathf.PI * 0.5f);
                card.anchoredPosition = basePos + Vector2.up * y;
                card.localScale = baseScale * (1f + punch * 0.075f);
                card.localRotation = baseRotation * Quaternion.Euler(-7.5f * punch, 12f * punch, 1.6f * punch);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration - half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration - half);
                float y = (1f - Mathf.Sin(t * Mathf.PI * 0.5f)) * jumpHeight;
                float punch = 1f - EaseOutQuad(t);
                card.anchoredPosition = basePos + Vector2.up * y;
                card.localScale = baseScale * (1f + punch * 0.075f);
                card.localRotation = baseRotation * Quaternion.Euler(-7.5f * punch, 12f * punch, 1.6f * punch);
                yield return null;
            }

            card.anchoredPosition = basePos;
            card.localScale = baseScale;
            card.localRotation = baseRotation;
        }

        private IEnumerator SelectionBurstCoroutine(RectTransform cardRect, Color accent)
        {
            if (cardRect == null || _rippleParent == null) yield break;

            Vector2 localPos = WorldToCanvasLocal(cardRect) + new Vector2(0f, -74f);
            StartCoroutine(AnimateDepthWave(localPos, new Color(accent.r, accent.g, accent.b, 0.44f),
                120f, 0f, GameConfig.AnimationDuration(0.24f)));
            StartCoroutine(SpawnPixelSparks(localPos + new Vector2(0f, 36f), accent, 6, 92f,
                GameConfig.AnimationDuration(0.30f)));

            float wait = GameConfig.AnimationDuration(0.30f);
            while (wait > 0f)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator ConfirmCardPopCoroutine(RectTransform cardRect, Color accent)
        {
            if (cardRect == null) yield break;

            Vector2 basePos = cardRect.anchoredPosition;
            Vector3 baseScale = cardRect.localScale;
            Quaternion baseRotation = cardRect.localRotation;
            float duration = GameConfig.AnimationDuration(0.36f);
            float elapsed = 0f;
            bool spawnedWave = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float lift = Mathf.Sin(t * Mathf.PI) * 24f;
                float punch = Mathf.Sin(t * Mathf.PI);
                cardRect.anchoredPosition = basePos + new Vector2(0f, lift);
                cardRect.localScale = baseScale * (1f + punch * 0.18f);
                cardRect.localRotation = baseRotation * Quaternion.Euler(-11f * punch, 18f * punch, 2.5f * punch);

                if (!spawnedWave && t > 0.18f && _rippleParent != null)
                {
                    spawnedWave = true;
                    Vector2 localPos = WorldToCanvasLocal(cardRect) + new Vector2(0f, -82f);
                    StartCoroutine(AnimateDepthWave(localPos, new Color(accent.r, accent.g, accent.b, 0.50f),
                        220f, 0f, GameConfig.AnimationDuration(0.34f)));
                }

                yield return null;
            }

            cardRect.anchoredPosition = basePos;
            cardRect.localScale = baseScale * 1.08f;
            cardRect.localRotation = baseRotation;
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
                    yield return ConfirmCardPopCoroutine(cardRect, character.AccentColor());
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

            StartCoroutine(AnimateDepthWave(localCenter + new Vector2(0f, -80f), new Color(accent.r, accent.g, accent.b, 0.54f),
                520f, 0f, GameConfig.AnimationDuration(0.48f)));
            StartCoroutine(AnimateRipple(localCenter, c1, 0f, 420f, GameConfig.AnimationDuration(0.38f)));
            StartCoroutine(AnimateRipple(localCenter, c2, 0.06f, 520f, GameConfig.AnimationDuration(0.40f)));
            StartCoroutine(AnimateRipple(localCenter, c3, 0.12f, 620f, GameConfig.AnimationDuration(0.42f)));
            StartCoroutine(SpawnPixelSparks(localCenter, accent, 12, 280f, GameConfig.AnimationDuration(0.52f)));

            float wait = GameConfig.AnimationDuration(0.58f);
            while (wait > 0f)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateRipple(Vector2 localCenter, Color color, float delay, float maxSize, float duration)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(GameConfig.AnimationDuration(delay));

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
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.15f, maxSize / 40f, EaseOutQuad(t));
                rt.localScale = Vector3.one * scale;
                float alpha = color.a * (1f - t);
                img.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(go);
        }

        private IEnumerator AnimateDepthWave(Vector2 localCenter, Color color, float maxWidth, float delay, float duration)
        {
            if (_rippleParent == null) yield break;
            if (delay > 0f) yield return new WaitForSecondsRealtime(GameConfig.AnimationDuration(delay));

            var go = new GameObject("DepthWave", typeof(RectTransform));
            go.transform.SetParent(_rippleParent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = localCenter;
            rt.sizeDelta = new Vector2(72f, 28f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutQuad(t);
                rt.sizeDelta = new Vector2(Mathf.Lerp(72f, maxWidth, eased), Mathf.Lerp(28f, maxWidth * 0.34f, eased));
                img.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
                yield return null;
            }

            Destroy(go);
        }

        private IEnumerator SpawnPixelSparks(Vector2 localCenter, Color accent, int count, float distance, float duration)
        {
            if (_rippleParent == null) yield break;

            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f / count) * i + 0.18f;
                float travel = distance * (0.56f + (i % 3) * 0.17f);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                StartCoroutine(AnimateSpark(localCenter, dir, travel, accent, duration));
            }

            float wait = duration;
            while (wait > 0f)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateSpark(Vector2 localCenter, Vector2 direction, float distance, Color accent, float duration)
        {
            if (_rippleParent == null) yield break;

            var go = new GameObject("PixelSpark", typeof(RectTransform));
            go.transform.SetParent(_rippleParent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = BoardVisualUtility.GetPixelSparkleSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = new Color(accent.r, accent.g, accent.b, 0.86f);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(16f, 16f);
            rt.anchoredPosition = localCenter;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutQuad(t);
                rt.anchoredPosition = localCenter + direction * distance * eased;
                rt.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.25f, t);
                rt.localRotation = Quaternion.Euler(0f, 0f, t * 180f);
                img.color = new Color(accent.r, accent.g, accent.b, 0.86f * (1f - t));
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
