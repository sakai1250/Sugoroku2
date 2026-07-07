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
            _group.blocksRaycasts = false;

            yield return Fade(0f, 1f, 0.12f);
            yield return new WaitForSeconds(GameConfigDuration(data.duration));
            yield return Fade(1f, 0f, 0.18f);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float duration = GameConfigDuration(seconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            _group.alpha = to;
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
            scaler.referenceResolution = new Vector2(1280f, 720f);
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
            JapaneseFontProvider.Apply(_label);
        }
    }
}
