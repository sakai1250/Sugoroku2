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
            rt.anchoredPosition = new Vector2(0f, -(ResourceHudVisuals.TopBarHeight + 8f));
            rt.sizeDelta = new Vector2(1100f, 64f);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
            bg.raycastTarget = false;

            _group = root.AddComponent<CanvasGroup>();

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 6f);
            textRt.offsetMax = new Vector2(-16f, -6f);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            HudTextStyle.ApplyReadable(_label, 26f, new Color(1f, 0.98f, 0.82f), true);
            SetMessage("ゲーム準備中…");
        }

        public static void Show(string message)
        {
            if (Instance == null) return;
            Instance.SetMessage(message);
        }

        private void SetMessage(string message)
        {
            if (_label != null) _label.text = message ?? "";
            if (_group != null) _group.alpha = string.IsNullOrEmpty(message) ? 0f : 1f;
        }

        private void OnTurnStarted(PlayerData player)
        {
            if (player == null) return;
            SetMessage($"{PlayerIdentity.FormatHudLabel(player)} のターン");
        }

        private void OnSquareEffect(PlayerData player, string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                SetMessage(msg);
        }

        private void OnEventTriggered(EventMaster ev, PlayerData player)
        {
            if (ev == null) return;
            string who = player != null ? PlayerIdentity.FormatHudLabel(player) : "";
            SetMessage(string.IsNullOrEmpty(who)
                ? $"イベント: {ev.Title} — 選択肢を選んでください"
                : $"{who} — イベント「{ev.Title}」— 選択肢を選んでください");
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
                    SetMessage($"{name} — 右下の「ダイスを振る」を押してください");
                    break;
                case TurnState.WaitAction when player.IsCpu:
                    SetMessage($"{name}（CPU）が考えています…");
                    break;
                case TurnState.Moving:
                    SetMessage($"{name} — 移動中…");
                    break;
                case TurnState.MassCheck:
                    SetMessage($"{name} — マス効果を確認中…");
                    break;
                case TurnState.Event:
                    // イベント本文は OnEventTriggered で上書き
                    if (_label == null || string.IsNullOrEmpty(_label.text) ||
                        !_label.text.Contains("イベント"))
                        SetMessage($"{name} — イベント処理中…");
                    break;
                case TurnState.TurnStart:
                    SetMessage($"{name} のターン開始");
                    break;
            }
        }
    }
}
