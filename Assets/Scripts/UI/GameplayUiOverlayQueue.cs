using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>
    /// ゲーム中の演出ポップアップを1本化するキュー。
    /// 全画面暗幕付きの専用Canvasを持ち、複数Coroutineからの表示要求を順番に処理する。
    /// </summary>
    public class GameplayUiOverlayQueue : MonoBehaviour
    {
        public static GameplayUiOverlayQueue Instance { get; private set; }

        private readonly Queue<PopupData> _queue = new();
        private CanvasGroup _group;
        private Image _dimmer;
        private TextMeshProUGUI _label;
        private RectTransform _cardRt;
        private Coroutine _runner;

        private struct PopupData
        {
            public string message;
            public Color color;
            public float duration;
            public bool dim;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildOverlayCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void ClearPending()
        {
            if (Instance == null) return;
            Instance._queue.Clear();
            if (Instance._runner != null)
            {
                Instance.StopCoroutine(Instance._runner);
                Instance._runner = null;
            }
            if (Instance._group != null)
            {
                Instance._group.alpha = 0f;
                Instance._group.blocksRaycasts = false;
            }
        }

        public static void Enqueue(string message, Color? color = null, float duration = 0.9f, bool dim = true)
        {
            if (Instance == null || string.IsNullOrEmpty(message)) return;
            if (EventModalUI.HasVisibleModal) return;
            Instance.EnqueueInternal(new PopupData
            {
                message = message,
                color = color ?? new Color(1f, 0.96f, 0.78f, 1f),
                duration = duration,
                dim = dim
            });
        }

        private void EnqueueInternal(PopupData data)
        {
            while (_queue.Count >= 3)
                _queue.Dequeue();
            _queue.Enqueue(data);
            if (_runner == null)
                _runner = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
                yield return ShowOne(data);
            }

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
            }
            _runner = null;
        }

        private IEnumerator ShowOne(PopupData data)
        {
            if (_group == null || _label == null) yield break;

            _label.text = data.message;
            _label.color = data.color;
            _dimmer.color = data.dim ? new Color(0f, 0f, 0f, 0.60f) : new Color(0f, 0f, 0f, 0.18f);
            _dimmer.raycastTarget = data.dim;
            _group.blocksRaycasts = data.dim;

            if (_cardRt != null)
            {
                _cardRt.localScale = new Vector3(0.94f, 0.94f, 1f);
                _cardRt.anchoredPosition = new Vector2(0f, 36f);
            }
            UpdateCardHeight();

            // 入場: フェード＋上へスライド＋スケール収束（他のカットイン演出と質感を統一）
            float enter = GameConfigDuration(0.18f);
            float e = 0f;
            while (e < enter)
            {
                e += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(e / enter);
                float f = JuiceMath.EaseOutCubic(t);
                if (_group != null) _group.alpha = f;
                if (_cardRt != null)
                {
                    _cardRt.localScale = Vector3.Lerp(new Vector3(0.94f, 0.94f, 1f), Vector3.one, f);
                    _cardRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(36f, 0f, f));
                }
                yield return null;
            }
            if (_group != null) _group.alpha = 1f;
            if (_cardRt != null)
            {
                _cardRt.localScale = Vector3.one;
                _cardRt.anchoredPosition = Vector2.zero;
            }

            yield return new WaitForSeconds(GameConfigDuration(data.duration));

            // 退場: 下へ少し沈みながらフェードアウト
            float exit = GameConfigDuration(0.14f);
            float x = 0f;
            while (x < exit)
            {
                x += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(x / exit);
                float f = JuiceMath.EaseOutCubic(t);
                if (_group != null) _group.alpha = 1f - f;
                if (_cardRt != null)
                    _cardRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -24f, f));
                yield return null;
            }
            if (_group != null) _group.alpha = 0f;
        }

        private void UpdateCardHeight()
        {
            if (_cardRt == null || _label == null) return;
            _label.ForceMeshUpdate();
            float preferred = _label.preferredHeight;
            float h = Mathf.Clamp(preferred + 24f, 150f, 220f);
            _cardRt.sizeDelta = new Vector2(_cardRt.sizeDelta.x, h);
        }

        private static float GameConfigDuration(float seconds) =>
            Sugoroku.Data.GameConfig.AnimationDuration(seconds);

        private void BuildOverlayCanvas()
        {
            var root = new GameObject("EventOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerManager.SortToastOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiSafeLayout.ReferenceWidth, UiSafeLayout.ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _group = root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var dimGo = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(root.transform, false);
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            _dimmer = dimGo.GetComponent<Image>();
            _dimmer.color = new Color(0f, 0f, 0f, 0.60f);
            _dimmer.raycastTarget = false;

            var card = new GameObject("PopupCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(840f, 150f);
            cardRt.anchoredPosition = Vector2.zero;
            _cardRt = cardRt;
            GameUiChrome.ApplySurface(card.transform, new Color(0.11f, 0.14f, 0.20f, 0.96f));
            GameUiChrome.ApplyAccentRail(card.transform, new Color(0.86f, 0.68f, 0.28f, 0.92f), 5f);

            var textGo = new GameObject("PopupText", typeof(RectTransform));
            textGo.transform.SetParent(card.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(30f, 12f);
            textRt.offsetMax = new Vector2(-30f, -12f);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            HudTextStyle.ApplyReadable(_label, HudTextStyle.Scale(30f), new Color(1f, 0.96f, 0.78f), true);
            HudTextStyle.ApplyOutlineSafe(_label, 0.14f, new Color(0f, 0f, 0f, 0.82f));
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 20f;
            _label.fontSizeMax = HudTextStyle.Scale(30f);
            JapaneseFontProvider.Apply(_label);
        }
    }
}
