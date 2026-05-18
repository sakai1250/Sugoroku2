using System;
using Sugoroku.Data;

namespace Sugoroku.Network
{
    /// <summary>Photon Fusion 実装の差し替えポイント（requirements §6.1）。</summary>
    public interface INetworkSessionBackend
    {
        bool IsOnline { get; }
        bool HasStateAuthority { get; }
        int  LocalPlayerIndex { get; }

        bool IsLocalHumanTurn(int currentPlayerIndex, PlayerData currentPlayer);

        /// <summary>StateAuthority がサイコロ出目を決定（ホストのみ乱数）。</summary>
        int RollDiceOnAuthority();

        /// <summary>StateAuthority が inclusive 範囲の乱数を生成（§7.3）。</summary>
        int RollRangeOnAuthority(int minInclusive, int maxInclusive);

        /// <summary>StateAuthority がイベントプールから1件抽選（§7.3）。</summary>
        string DrawEventIdOnAuthority();

        void BroadcastDiceRoll(int playerIndex, int result);
        void BroadcastEventDraw(int playerIndex, string eventId);
        void BroadcastJournalIfGain(int playerIndex, int ifGain);
        void BroadcastEventChoice(int playerIndex, string eventId, int choiceIndex);
        void BroadcastTurnAdvanced(int nextPlayerIndex);

        NetworkedGameState CaptureState();
        void ApplyState(NetworkedGameState state);

        event Action<int, int> OnDiceRollSynced;
        event Action<int, string> OnEventDrawSynced;
        event Action<int, int> OnJournalIfGainSynced;
        event Action<int, string, int> OnEventChoiceSynced;
        event Action<int> OnTurnAdvancedSynced;
        event Action<NetworkedGameState> OnGameStateSynced;
    }
}
