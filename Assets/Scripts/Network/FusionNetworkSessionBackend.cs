#if PHOTON_FUSION
using System;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Network
{
    /// <summary>
    /// Photon Fusion 用バックエンド（PHOTON_FUSION 定義時のみコンパイル）。
    /// NetworkRunner / [Networked] / Rpc をここに実装する。
    /// </summary>
    public class FusionNetworkSessionBackend : INetworkSessionBackend
    {
        // TODO Phase D:
        // - NetworkRunner を保持し HasStateAuthority => Runner.IsServer または Object.HasStateAuthority
        // - [Networked] NetworkedGameState を NetworkBehaviour 上に配置
        // - [Rpc(RpcSources.StateAuthority, RpcTargets.All)] Rpc_BroadcastDiceRoll(int result)
        // - [Rpc] Rpc_SubmitEventChoice(string eventId, int choiceIndex)
        // - OnPlayerJoined 時に CaptureState を新規参加者へ送信（再接続）

        public bool IsOnline => false;
        public bool HasStateAuthority => false;
        public int  LocalPlayerIndex => 0;

        public event Action<int, int> OnDiceRollSynced;
        public event Action<int, string> OnEventDrawSynced;
        public event Action<int, int> OnJournalIfGainSynced;
        public event Action<int, string, int> OnEventChoiceSynced;
        public event Action<int> OnTurnAdvancedSynced;
        public event Action<NetworkedGameState> OnGameStateSynced;

        public bool TryInitialize()
        {
            Debug.LogWarning("FusionNetworkSessionBackend: Fusion Runner の接続処理を実装してください。");
            return false;
        }

        public bool IsLocalHumanTurn(int currentPlayerIndex, PlayerData currentPlayer) => false;
        public int RollDiceOnAuthority() => 0;
        public int RollRangeOnAuthority(int minInclusive, int maxInclusive) => minInclusive;
        public string DrawEventIdOnAuthority() => null;
        public void BroadcastDiceRoll(int playerIndex, int result) { }
        public void BroadcastEventDraw(int playerIndex, string eventId) { }
        public void BroadcastJournalIfGain(int playerIndex, int ifGain) { }
        public void BroadcastEventChoice(int playerIndex, string eventId, int choiceIndex) { }
        public void BroadcastTurnAdvanced(int nextPlayerIndex) { }
        public NetworkedGameState CaptureState() => default;
        public void ApplyState(NetworkedGameState state) { }
    }
}
#endif
