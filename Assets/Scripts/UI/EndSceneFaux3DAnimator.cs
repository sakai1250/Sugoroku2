using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>修了／ゲームオーバー画面の疑似3Dレイヤーを軽く動かす。</summary>
    [DisallowMultipleComponent]
    public class EndSceneFaux3DAnimator : MonoBehaviour
    {
        private readonly Track _cloud = new();
        private readonly Track _foreground = new();
        private readonly Track _floor = new();
        private readonly Track _careerCard = new();
        private readonly Track _impactPlate = new();
        private readonly Track _accentPlate = new();

        private bool _danger;
        private float _startTime;

        public void Configure(bool danger)
        {
            _danger = danger;
            _startTime = Time.unscaledTime;
            BindAll(resetBase: true);
        }

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            BindAll(resetBase: true);
        }

        private void LateUpdate()
        {
            BindAll(resetBase: false);

            float time = Time.unscaledTime - _startTime;
            AnimateParallax(_cloud, new Vector2(18f, 4f), time * 0.24f);
            AnimateParallax(_foreground, new Vector2(-28f, 0f), time * 0.18f);
            AnimatePulse(_floor, time);
            AnimateCard(_careerCard, time, _danger ? 0.55f : 1f);
            AnimateCard(_impactPlate, time, _danger ? 1f : 0.5f);
            AnimateAccent(_accentPlate, time);
        }

        private void BindAll(bool resetBase)
        {
            Bind(_cloud, "EndSceneCloudParallax", resetBase);
            Bind(_foreground, "EndSceneForegroundParallax", resetBase);
            Bind(_floor, "EndSceneFloorShadow", resetBase);
            Bind(_careerCard, "CareerDecision3DCard", resetBase);
            Bind(_impactPlate, "GameOverImpactPlate", resetBase);
            Bind(_accentPlate, "EndSceneAccentPlate", resetBase);
        }

        private void Bind(Track track, string objectName, bool resetBase)
        {
            if (track.Rect == null)
            {
                var t = transform.Find(objectName);
                if (t != null)
                {
                    track.Rect = t as RectTransform;
                    track.Image = t.GetComponent<Image>();
                    resetBase = true;
                }
            }

            if (track.Rect == null || !resetBase) return;

            track.BasePos = track.Rect.anchoredPosition;
            track.BaseScale = track.Rect.localScale;
            track.BaseRotation = track.Rect.localRotation;
            track.BaseColor = track.Image != null ? track.Image.color : Color.white;
            track.HasBase = true;
        }

        private static void AnimateParallax(Track track, Vector2 range, float phase)
        {
            if (!track.Ready) return;

            float x = Mathf.Sin(phase) * range.x;
            float y = Mathf.Cos(phase * 0.7f) * range.y;
            track.Rect.anchoredPosition = track.BasePos + new Vector2(x, y);
        }

        private static void AnimatePulse(Track track, float time)
        {
            if (!track.Ready) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.8f);
            track.Rect.localScale = track.BaseScale * (0.98f + pulse * 0.04f);
            if (track.Image != null)
            {
                var c = track.BaseColor;
                c.a *= 0.86f + pulse * 0.18f;
                track.Image.color = c;
            }
        }

        private static void AnimateCard(Track track, float time, float strength)
        {
            if (!track.Ready) return;

            float intro = Mathf.Clamp01(time / 0.42f);
            float easedIntro = 1f - Mathf.Pow(1f - intro, 3f);
            float hover = Mathf.Sin(time * 1.55f) * 4f * strength;
            float tiltY = Mathf.Sin(time * 0.95f) * 5.5f * strength;
            float tiltX = Mathf.Cos(time * 1.10f) * 2.6f * strength;
            float roll = Mathf.Sin(time * 1.25f) * 0.8f * strength;

            track.Rect.anchoredPosition = track.BasePos + new Vector2(0f, hover);
            track.Rect.localScale = track.BaseScale * Mathf.Lerp(0.86f, 1f, easedIntro);
            track.Rect.localRotation = track.BaseRotation *
                Quaternion.Euler(tiltX * easedIntro, tiltY * easedIntro, roll * easedIntro);
        }

        private static void AnimateAccent(Track track, float time)
        {
            if (!track.Ready || track.Image == null) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2.4f);
            var c = track.BaseColor;
            c.a *= 0.72f + pulse * 0.28f;
            track.Image.color = c;
        }

        private sealed class Track
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 BasePos;
            public Vector3 BaseScale;
            public Quaternion BaseRotation;
            public Color BaseColor;
            public bool HasBase;

            public bool Ready => Rect != null && HasBase;
        }
    }
}
