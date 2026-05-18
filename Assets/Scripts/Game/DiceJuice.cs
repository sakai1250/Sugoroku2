using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.Game
{
    /// <summary>サイコロ振りの共有アニメーション（ワールド駒 / HUD アイコン）。</summary>
    public static class DiceJuice
    {
        public const int RollTicks       = 28;
        public const float TickInterval  = 0.045f;
        public const float SettleDuration = 0.32f;

        public static IEnumerator PlaySpriteRoll(
            Sprite[] faces,
            int finalValue,
            Action<Sprite> setSprite,
            Action<int> onFaceShown,
            Action<TransformJuiceState> applyTransform,
            TransformJuiceState baseState)
        {
            if (faces == null || faces.Length < 6 || setSprite == null || applyTransform == null)
            {
                onFaceShown?.Invoke(finalValue);
                yield break;
            }

            int finalIndex = Mathf.Clamp(finalValue, 1, 6) - 1;

            for (int i = 0; i <= RollTicks; i++)
            {
                float t = i / (float)RollTicks;
                float eased = EaseOutCubic(t);

                int side = i < RollTicks
                    ? UnityEngine.Random.Range(0, 6)
                    : finalIndex;

                setSprite(faces[side]);
                onFaceShown?.Invoke(side + 1);

                float spin = Mathf.Lerp(720f, 1080f, eased);
                float wobble = Mathf.Sin(t * Mathf.PI * 6f) * (1f - eased) * 12f;
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                float squash = 1f + Mathf.Sin(t * Mathf.PI * 8f) * 0.12f * (1f - eased);
                float hop = Mathf.Sin(t * Mathf.PI) * 0.35f * (1f - eased);

                applyTransform(new TransformJuiceState
                {
                    rotationZ   = spin + wobble,
                    scaleMul    = pulse,
                    scaleSquash = squash,
                    offsetY     = hop,
                    flash       = Mathf.Lerp(1f, 1.35f, Mathf.Sin(t * Mathf.PI * 4f) * (1f - eased))
                });

                float wait = Mathf.Lerp(TickInterval * 1.2f, TickInterval * 0.55f, eased);
                yield return new WaitForSeconds(wait);
            }

            setSprite(faces[finalIndex]);
            onFaceShown?.Invoke(finalValue);

            float elapsed = 0f;
            while (elapsed < SettleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SettleDuration;
                float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2.5f)) * (1f - t) * 0.2f;
                float punch = 1f + (1f - t) * (1f - t) * 0.18f;

                applyTransform(new TransformJuiceState
                {
                    rotationZ   = Mathf.Lerp(1080f, 0f, EaseOutBack(t)),
                    scaleMul    = Mathf.Lerp(1.15f, 1f, EaseOutBack(t)) * punch,
                    scaleSquash = 1f,
                    offsetY     = bounce,
                    flash       = Mathf.Lerp(1.25f, 1f, t)
                });
                yield return null;
            }

            applyTransform(baseState);
        }

        public static IEnumerator PlayHudRoll(Image diceImage, RectTransform rect, int finalValue, Action<int> onFaceShown)
        {
            if (diceImage == null)
            {
                onFaceShown?.Invoke(finalValue);
                yield break;
            }

            var faces = KenneyAssets.LoadDiceFaces("dieWhite");
            var basePos = rect != null ? rect.anchoredPosition : Vector2.zero;
            var baseScale = rect != null ? rect.localScale : Vector3.one;
            var baseRot = rect != null ? rect.localEulerAngles : Vector3.zero;

            yield return PlaySpriteRoll(
                faces,
                finalValue,
                sp =>
                {
                    if (sp != null)
                    {
                        diceImage.sprite = sp;
                        diceImage.preserveAspect = true;
                        diceImage.color = Color.white;
                    }
                },
                onFaceShown,
                state =>
                {
                    if (rect != null)
                    {
                        rect.localEulerAngles = new Vector3(0f, 0f, state.rotationZ);
                        float s = state.scaleMul;
                        float sq = state.scaleSquash;
                        rect.localScale = new Vector3(baseScale.x * s / sq, baseScale.y * s * sq, baseScale.z);
                        rect.anchoredPosition = basePos + new Vector2(0f, state.offsetY * 40f);
                    }

                    diceImage.color = new Color(state.flash, state.flash, state.flash, 1f);
                },
                TransformJuiceState.Identity);
        }

        public static IEnumerator PunchScale(Transform target, float peak, float duration)
        {
            if (target == null) yield break;
            var baseScale = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = Mathf.Lerp(peak, 1f, EaseOutBack(t));
                target.localScale = baseScale * s;
                yield return null;
            }
            target.localScale = baseScale;
        }

        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    public struct TransformJuiceState
    {
        public float rotationZ;
        public float scaleMul;
        public float scaleSquash;
        public float offsetY;
        public float flash;

        public static TransformJuiceState Identity => new()
        {
            rotationZ = 0f,
            scaleMul = 1f,
            scaleSquash = 1f,
            offsetY = 0f,
            flash = 1f
        };
    }
}
