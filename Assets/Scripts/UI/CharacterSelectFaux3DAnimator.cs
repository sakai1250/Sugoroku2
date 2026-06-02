using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>キャラ選択画面のピクセル背景とカードデッキに、薄い疑似3Dモーションを加える。</summary>
    [DisallowMultipleComponent]
    public class CharacterSelectFaux3DAnimator : MonoBehaviour
    {
        private readonly Track _cloud = new();
        private readonly Track _foreground = new();
        private readonly Track _floor = new();
        private readonly Track _cardParent = new();
        private readonly Track _deckPanel = new();
        private readonly Track _infoPanel = new();
        private readonly Track _portraitFrame = new();
        private readonly Track _accentPlate = new();
        private readonly Track _roleBadges = new();
        private readonly Track _meters = new();
        private readonly Track _playerBadge = new();
        private readonly Track _pagerPips = new();
        private readonly Track _focusPlaque = new();

        private Color _accent = new(0.86f, 0.68f, 0.28f, 1f);
        private float _startTime;

        public void Configure(Color accent)
        {
            _accent = accent;
            _startTime = Time.unscaledTime;
            BindAll(resetBase: true);
        }

        public void SetAccent(Color accent)
        {
            _accent = accent;
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
            AnimateParallax(_cloud, new Vector2(18f, 4f), time * 0.23f);
            AnimateParallax(_foreground, new Vector2(-26f, 0f), time * 0.18f);
            AnimatePulse(_floor, time, 0.96f, 1.04f);
            AnimateCardDeck(_cardParent, time);
            AnimatePanel(_deckPanel, time, 0.6f);
            AnimatePanel(_infoPanel, time + 0.8f, 0.42f);
            AnimatePanel(_portraitFrame, time + 1.3f, 0.55f);
            AnimatePanel(_roleBadges, time + 0.35f, 0.44f);
            AnimatePanel(_meters, time + 1.1f, 0.34f);
            AnimatePanel(_playerBadge, time + 0.2f, 0.38f);
            AnimatePanel(_focusPlaque, time + 0.6f, 0.36f);
            AnimatePanel(_pagerPips, time + 1.4f, 0.46f);
            AnimatePagerPips(time);
            AnimateCharacterCards(time);
            AnimateAccent(_accentPlate, time);
        }

        private void BindAll(bool resetBase)
        {
            Bind(_cloud, "CharacterSelectCloudParallax", resetBase);
            Bind(_foreground, "CharacterSelectForegroundParallax", resetBase);
            Bind(_floor, "CharacterSelectFloorShadow", resetBase);
            Bind(_cardParent, "CardParent", resetBase);
            Bind(_deckPanel, "CardDeckPanel", resetBase);
            Bind(_infoPanel, "CharacterInfoPanel", resetBase);
            Bind(_portraitFrame, "PortraitFrame", resetBase);
            Bind(_accentPlate, "CharacterSelectAccentPlate", resetBase);
            Bind(_roleBadges, "CharacterRoleBadgeGroup", resetBase);
            Bind(_meters, "ProfileMeterGroup", resetBase);
            Bind(_playerBadge, "PlayerSlotBadge", resetBase);
            Bind(_pagerPips, "CharacterPagerPips", resetBase);
            Bind(_focusPlaque, "CharacterFocusPlaque", resetBase);
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
            float y = Mathf.Cos(phase * 0.72f) * range.y;
            track.Rect.anchoredPosition = track.BasePos + new Vector2(x, y);
        }

        private static void AnimatePulse(Track track, float time, float minScale, float maxScale)
        {
            if (!track.Ready) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.65f);
            track.Rect.localScale = track.BaseScale * Mathf.Lerp(minScale, maxScale, pulse);
            if (track.Image != null)
            {
                var c = track.BaseColor;
                c.a *= 0.86f + pulse * 0.14f;
                track.Image.color = c;
            }
        }

        private static void AnimateCardDeck(Track track, float time)
        {
            if (!track.Ready) return;

            float hover = Mathf.Sin(time * 1.25f) * 3.5f;
            float tiltY = Mathf.Sin(time * 0.72f) * 2.8f;
            float tiltX = Mathf.Cos(time * 0.86f) * 1.2f;
            track.Rect.anchoredPosition = track.BasePos + new Vector2(0f, hover);
            track.Rect.localRotation = track.BaseRotation * Quaternion.Euler(tiltX, tiltY, 0f);
        }

        private static void AnimatePanel(Track track, float time, float strength)
        {
            if (!track.Ready) return;

            float y = Mathf.Sin(time * 1.1f) * 2f * strength;
            float roll = Mathf.Sin(time * 0.82f) * 0.35f * strength;
            track.Rect.anchoredPosition = track.BasePos + new Vector2(0f, y);
            track.Rect.localRotation = track.BaseRotation * Quaternion.Euler(0f, 0f, roll);
        }

        private void AnimateAccent(Track track, float time)
        {
            if (!track.Ready || track.Image == null) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2.3f);
            track.Image.color = new Color(_accent.r, _accent.g, _accent.b, 0.62f + pulse * 0.24f);
        }

        private void AnimateCharacterCards(float time)
        {
            if (!_cardParent.Ready) return;

            int index = 0;
            foreach (Transform child in _cardParent.Rect)
            {
                if (child is not RectTransform card)
                {
                    index++;
                    continue;
                }

                bool selected = card.localScale.x > 1.04f;
                float phase = time * (selected ? 2.7f : 1.7f) + index * 0.64f;
                float bob = Mathf.Sin(phase) * (selected ? 2.4f : 0.7f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(phase * 1.25f);

                SetLayerY(card, "SelectedTab", 3f + (selected ? bob * 0.36f : 0f));
                SetLayerY(card, "PortraitWindow", 35f + bob * 0.55f);
                SetLayerY(card, "Portrait", 39f + bob);
                SetLayerScale(card, "CharacterNumberBadge", selected ? 1f + pulse * 0.06f : 1f);
                SetLayerScale(card, "SelectedCardHalo", selected ? 1f + pulse * 0.10f : 1f);
                SetLayerScale(card, "SelectedTab", selected ? 1f + pulse * 0.04f : 0.96f);
                SetLayerScale(card, "SelectionMeter", selected ? 1f + pulse * 0.08f : 1f);
                RotateLayer(card, "CardSparkleA", time * (selected ? 90f : 36f) + index * 21f);
                RotateLayer(card, "CardSparkleB", -time * (selected ? 74f : 28f) - index * 18f);
                PulseLayerAlpha(card, "SelectedCardHalo", selected ? 0.20f : 0f, selected ? 0.18f : 0f, pulse);
                PulseLayerAlpha(card, "CharacterNumberBadge", selected ? 0.86f : 0.56f, selected ? 0.12f : 0.06f, pulse);
                PulseLayerAlpha(card, "SelectedTab", selected ? 0.78f : 0f, selected ? 0.20f : 0f, pulse);
                PulseLayerAlpha(card, "CardTopRule", selected ? 0.76f : 0.46f, selected ? 0.24f : 0.08f, pulse);
                PulseLayerAlpha(card, "CardSparkleA", selected ? 0.48f : 0.18f, selected ? 0.34f : 0.10f, pulse);
                PulseLayerAlpha(card, "CardSparkleB", selected ? 0.48f : 0.16f, selected ? 0.34f : 0.10f, 1f - pulse);

                index++;
            }
        }

        private void AnimatePagerPips(float time)
        {
            if (!_pagerPips.Ready) return;

            int index = 0;
            foreach (Transform child in _pagerPips.Rect)
            {
                if (!child.name.StartsWith("PagerPip_", System.StringComparison.Ordinal) ||
                    child is not RectTransform pip)
                    continue;

                bool selected = pip.sizeDelta.x > 30f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3.2f + index * 0.38f);
                pip.localScale = Vector3.one * (selected ? 1f + pulse * 0.10f : 1f);
                RotateLayer(pip, "Spark", time * (selected ? 120f : 0f));
                PulseLayerAlpha(pip, "Spark", selected ? 0.52f : 0f, selected ? 0.36f : 0f, pulse);
                index++;
            }
        }

        private static void SetLayerY(RectTransform card, string childName, float y)
        {
            var rt = card.Find(childName) as RectTransform;
            if (rt == null) return;
            var pos = rt.anchoredPosition;
            pos.y = y;
            rt.anchoredPosition = pos;
        }

        private static void SetLayerScale(RectTransform card, string childName, float scaleX)
        {
            var rt = card.Find(childName) as RectTransform;
            if (rt == null) return;
            rt.localScale = new Vector3(scaleX, 1f, 1f);
        }

        private static void RotateLayer(RectTransform card, string childName, float angle)
        {
            var rt = card.Find(childName) as RectTransform;
            if (rt == null) return;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static void PulseLayerAlpha(RectTransform card, string childName, float baseAlpha, float range, float t)
        {
            var img = card.Find(childName)?.GetComponent<Image>();
            if (img == null) return;

            var c = img.color;
            c.a = Mathf.Clamp01(baseAlpha + range * t);
            img.color = c;
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
