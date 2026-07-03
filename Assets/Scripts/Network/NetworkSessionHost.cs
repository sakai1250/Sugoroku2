using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.Network
{
    /// <summary>
    /// requirements §6.1: NetworkRunner / StateAuthority / RPC / 再接続の統合窓口。
    /// 現状は Local バックエンド。Fusion 導入時は FusionNetworkSessionBackend に差し替え。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class NetworkSessionHost : MonoBehaviour
    {
        public static NetworkSessionHost Instance { get; private set; }

        [SerializeField] private bool useFusionWhenAvailable;

        private INetworkSessionBackend _backend;

        public bool IsOnline           => _backend?.IsOnline ?? false;
        public bool HasStateAuthority  => _backend?.HasStateAuthority ?? true;
        public int  LocalPlayerIndex   => _backend?.LocalPlayerIndex ?? 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _backend = CreateBackend();
            _backend.OnDiceRollSynced       += HandleDiceRollSynced;
            _backend.OnEventDrawSynced      += HandleEventDrawSynced;
            _backend.OnJournalIfGainSynced  += HandleJournalIfGainSynced;
            _backend.OnEventChoiceSynced    += HandleEventChoiceSynced;
            _backend.OnTurnAdvancedSynced   += _ => { };
            _backend.OnGameStateSynced        += HandleGameStateSynced;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_backend == null) return;
            _backend.OnDiceRollSynced       -= HandleDiceRollSynced;
            _backend.OnEventDrawSynced      -= HandleEventDrawSynced;
            _backend.OnJournalIfGainSynced  -= HandleJournalIfGainSynced;
            _backend.OnEventChoiceSynced    -= HandleEventChoiceSynced;
            _backend.OnGameStateSynced        -= HandleGameStateSynced;
        }

        private INetworkSessionBackend CreateBackend()
        {
#if PHOTON_FUSION
            if (useFusionWhenAvailable)
            {
                var fusion = new FusionNetworkSessionBackend();
                if (fusion.TryInitialize())
                    return fusion;
            }
#endif
            return new LocalNetworkSessionBackend(GameSession.IsDailyChallenge ? GameSession.DailySeed : (int?)null);
        }

        public bool CanLocalPlayerRoll()
        {
            var gm = GameManager.Instance;
            var tm = TurnManager.Instance;
            if (gm == null || tm == null || !gm.IsInitialized) return false;
            if (tm.CurrentState != TurnState.WaitAction) return false;
            var p = gm.GetCurrentPlayer();
            return _backend.IsLocalHumanTurn(gm.GetCurrentPlayerIndex(), p);
        }

        /// <summary>§7.3 — inclusive 乱数（オンライン時は StateAuthority のみ）。</summary>
        public int RollRange(int minInclusive, int maxInclusive)
        {
            if (IsOnline && !HasStateAuthority)
            {
                Debug.LogWarning("RollRange: StateAuthority がありません。");
                return minInclusive;
            }
            return _backend.RollRangeOnAuthority(minInclusive, maxInclusive);
        }

        /// <summary>ジャーナルマスの IF 獲得（天才肌 +1d6・上限付き）。オンライン時は同期後に適用。</summary>
        public int RequestJournalIfGain(PlayerData player, int baseIfGain)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("RequestJournalIfGain: StateAuthority がありません。");
                return -1;
            }

            int ifGain = GeniusJournalRules.ComputeIfGain(
                baseIfGain, player, RollRange);

            var gm = GameManager.Instance;
            int playerIndex = gm?.GetCurrentPlayerIndex() ?? player?.Index ?? 0;

            if (IsOnline)
                _backend.BroadcastJournalIfGain(playerIndex, ifGain);
            return IsOnline ? -1 : ifGain;
        }

        /// <summary>イベントプール抽選（§7.3）。bound イベントは呼び出し側で渡す。</summary>
        public EventMaster RequestEventDraw()
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("RequestEventDraw: StateAuthority がありません。");
                return null;
            }

            string eventId = _backend.DrawEventIdOnAuthority();
            if (string.IsNullOrEmpty(eventId)) return null;

            var ev = EventManager.Instance?.GetById(eventId);
            var gm = GameManager.Instance;
            int playerIndex = gm?.GetCurrentPlayerIndex() ?? 0;

            if (IsOnline)
                _backend.BroadcastEventDraw(playerIndex, eventId);
            return IsOnline ? null : ev;
        }

        /// <summary>サイコロ（StateAuthority で出目決定。オンライン時は RPC 経由で全員が PlaySyncedRoll）。</summary>
        public int RequestDiceRoll()
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("RequestDiceRoll: StateAuthority がありません（クライアントは RPC を待つ）。");
                return -1;
            }

            var gm = GameManager.Instance;
            int playerIndex = gm?.GetCurrentPlayerIndex() ?? 0;
            int result = _backend.RollDiceOnAuthority();

            if (IsOnline)
                _backend.BroadcastDiceRoll(playerIndex, result);
            return IsOnline ? -1 : result;
        }

        public void SubmitEventChoice(int choiceIndex, string eventId)
        {
            if (IsOnline && !HasStateAuthority)
            {
                Debug.LogWarning("SubmitEventChoice: StateAuthority のみ送信可能です。");
                return;
            }

            var gm = GameManager.Instance;
            int playerIndex = gm?.GetCurrentPlayerIndex() ?? 0;

            if (IsOnline)
                _backend.BroadcastEventChoice(playerIndex, eventId, choiceIndex);
            else
                HandleEventChoiceSynced(playerIndex, eventId, choiceIndex);
        }

        public void NotifyTurnAdvanced(int nextPlayerIndex) =>
            _backend.BroadcastTurnAdvanced(nextPlayerIndex);

        public NetworkedGameState CaptureSnapshot() => _backend.CaptureState();

        public void RestoreSnapshot(NetworkedGameState state) => _backend.ApplyState(state);

        private void HandleEventDrawSynced(int playerIndex, string eventId)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.GetCurrentPlayerIndex() != playerIndex) return;
            var ev = EventManager.Instance?.GetById(eventId);
            var player = gm.GetCurrentPlayer();
            if (ev != null && player != null)
                EventManager.Instance.TriggerEvent(ev, player);
        }

        private void HandleJournalIfGainSynced(int playerIndex, int ifGain)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var players = gm.GetAllPlayers();
            if (playerIndex < 0 || playerIndex >= players.Length) return;
            TurnManager.Instance?.ApplyJournalIfGain(players[playerIndex], ifGain);
        }

        private void HandleDiceRollSynced(int playerIndex, int result)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsInitialized) return;
            if (gm.GetCurrentPlayerIndex() != playerIndex) return;
            DiceRoller.Instance?.PlaySyncedRoll(result);
        }

        private void HandleEventChoiceSynced(int playerIndex, string eventId, int choiceIndex)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var players = gm.GetAllPlayers();
            if (playerIndex < 0 || playerIndex >= players.Length) return;

            var ev = EventManager.Instance?.GetById(eventId);
            if (ev == null) return;

            var player = players[playerIndex];
            var choice = ev.GetChoice(choiceIndex);
            if (choice == null) return;

            EventManager.Instance.ApplyChoice(player, choice);
            if (TurnManager.Instance != null && gm.GetCurrentPlayerIndex() == playerIndex)
                TurnManager.Instance.EndTurn();
        }

        private void HandleGameStateSynced(NetworkedGameState state)
        {
            var gm = GameManager.Instance;
            if (gm == null || state.Players == null) return;

            var players = gm.GetAllPlayers();
            for (int i = 0; i < players.Length && i < state.Players.Length; i++)
                state.Players[i].ApplyTo(players[i]);

            Debug.Log($"NetworkSession: スナップショット復元（ターン={state.CurrentPlayerIndex}, 状態={state.TurnState}）");
        }
    }
}
