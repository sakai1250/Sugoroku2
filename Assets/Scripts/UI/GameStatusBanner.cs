using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>画面上部に「今何が起きているか」を大きく表示する。</summary>
    public class GameStatusBanner : MonoBehaviour
    {
        public static GameStatusBanner Instance { get; private set; }

        private TextMeshProUGUI _label;
        private CanvasGroup     _group;
        private bool            _subscribed;
        private bool            _suppressed;
        private string          _lastMessage = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildUi();
        }

        private void OnEnable()
        {
            if (!_subscribed) StartCoroutine(SubscribeWhenReady());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Unsubscribe();
        }

        private System.Collections.IEnumerator SubscribeWhenReady()
        {
            while (TurnManager.Instance == null || GameManager.Instance == null)
                yield return null;

            while (EventManager.Instance == null)
                yield return null;

            if (_subscribed) yield break;

            TurnManager.Instance.OnStateChanged += OnTurnState;
            TurnManager.Instance.OnTurnStarted  += OnTurnStarted;
            GameManager.Instance.OnSquareEffect += OnSquareEffect;
            EventManager.Instance.OnEventTriggered += OnEventTriggered;

            _subscribed = true;
            RefreshForCurrentState();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnStateChanged -= OnTurnState;
                TurnManager.Instance.OnTurnStarted  -= OnTurnStarted;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnSquareEffect -= OnSquareEffect;
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            _subscribed = false;
        }

        private void BuildUi()
        {
            var root = new GameObject("GameStatusBanner", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(ResourceHudVisuals.TopBarHeight + 6f));
            rt.sizeDelta = new Vector2(980f, 76f);

            var bg = root.AddComponent<Image>();
            bg.color = GameUiChrome.Surface;
            bg.raycastTarget = false;
            GameUiChrome.ApplySurface(root.transform, new Color(0.11f, 0.14f, 0.19f, 0.88f));
            GameUiChrome.ApplyAccentRail(root.transform, new Color(0.86f, 0.68f, 0.28f, 0.82f), 4f);

            _group = root.AddComponent<CanvasGroup>();

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 4f);
            textRt.offsetMax = new Vector2(-14f, -4f);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            HudTextStyle.ApplyReadable(_label, HudTextStyle.JuiceStatusFontSize, new Color(1f, 0.96f, 0.78f), true);
            HudTextStyle.ApplyOutlineSafe(_label, HudTextStyle.JuiceOutlineWidth, HudTextStyle.OutlineColor);
            SetMessage("ゲーム準備中…");
        }

        /// <summary>イベントモーダル表示中はバナーを隠し、選択肢との Z 重なりを防ぐ。</summary>
        public static void SetSuppressed(bool suppressed)
        {
            if (Instance == null) return;
            Instance._suppressed = suppressed;
            if (suppressed)
                Instance.SetMessage("", null, force: true);
            else if (!string.IsNullOrEmpty(Instance._lastMessage))
                Instance.SetMessage(Instance._lastMessage, null, force: true);
        }

        public static void Show(string message)
        {
            if (Instance == null) return;
            if (Instance._suppressed || EventModalUI.HasVisibleModal) return;
            Instance.SetMessage(message);
            GameplayUiOverlayQueue.Enqueue(message, dim: false);
        }

        private void SetMessage(string message, Color? color = null, bool force = false)
        {
            if (!force && _suppressed) return;
            _lastMessage = message ?? "";
            if (_label != null) _label.text = _lastMessage;
            if (_label != null && color.HasValue) _label.color = color.Value;
            if (_group != null) _group.alpha = string.IsNullOrEmpty(_lastMessage) ? 0f : 1f;
        }

        private void OnTurnStarted(PlayerData player)
        {
            if (player == null) return;
            SetMessage($"★ {PlayerIdentity.FormatHudLabel(player)} のターン!", new Color(1f, 0.95f, 0.70f, 1f));
        }

        private void OnSquareEffect(PlayerData player, string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                SetMessage(msg, new Color(0.72f, 1f, 0.76f, 1f));
        }

        private void OnEventTriggered(EventMaster ev, PlayerData player)
        {
            if (ev == null || _suppressed || EventModalUI.HasVisibleModal) return;
            string who = player != null ? PlayerIdentity.FormatHudLabel(player) : "";
            SetMessage(string.IsNullOrEmpty(who)
                ? $"★ イベント: {ev.Title}"
                : $"★ {who}  イベント「{ev.Title}」", new Color(1f, 0.72f, 0.90f, 1f));
        }

        private void OnTurnState(TurnState state) => RefreshForState(state);

        private void RefreshForCurrentState()
        {
            if (TurnManager.Instance != null)
                RefreshForState(TurnManager.Instance.CurrentState);
        }

        private void RefreshForState(TurnState state)
        {
            var player = GameManager.Instance?.GetCurrentPlayer();
            if (player == null) return;

            bool rolling = DiceRoller.Instance != null && DiceRoller.Instance.IsRolling;
            string name = PlayerIdentity.FormatHudLabel(player);

            switch (state)
            {
                case TurnState.WaitAction when !player.IsCpu && !rolling:
                    SetMessage($"★ {name}  ダイスを振ろう!", new Color(1f, 0.92f, 0.42f, 1f));
                    break;
                case TurnState.WaitAction when player.IsCpu:
                    SetMessage($"★ {name} CPUが考え中...", new Color(1f, 0.88f, 0.58f, 1f));
                    break;
                case TurnState.Moving:
                    SetMessage($">> {name}  移動中!", new Color(0.50f, 0.94f, 1f, 1f));
                    break;
                case TurnState.MassCheck:
                    SetMessage($"◇ {name}  マス確認!", new Color(0.72f, 1f, 0.62f, 1f));
                    break;
                case TurnState.Event:
                    if (_suppressed || EventModalUI.HasVisibleModal) break;
                    // イベント本文は OnEventTriggered で上書き
                    if (_label == null || string.IsNullOrEmpty(_label.text) ||
                        !_label.text.Contains("イベント"))
                        SetMessage($"★ {name}  イベント!", new Color(1f, 0.72f, 0.90f, 1f));
                    break;
                case TurnState.TurnStart:
                    SetMessage($"★ {name} のターン開始!", new Color(1f, 0.95f, 0.70f, 1f));
                    break;
            }
        }
    }
}
