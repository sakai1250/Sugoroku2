using System;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Network
{
    /// <summary>オフライン／CPU 対戦用。全権限をローカルが保持。</summary>
    public class LocalNetworkSessionBackend : INetworkSessionBackend
    {
        public bool IsOnline => false;
        public bool HasStateAuthority => true;
        public int  LocalPlayerIndex => 0;

        private readonly System.Random _seededRandom;

        public LocalNetworkSessionBackend() : this(null) { }

        /// <summary>seed が指定された場合、全乱数呼び出しを決定論的な System.Random に切り替える(デイリーチャレンジ用)。</summary>
        public LocalNetworkSessionBackend(int? seed)
        {
            _seededRandom = seed.HasValue ? new System.Random(seed.Value) : null;
        }

        public event Action<int, int> OnDiceRollSynced;
        public event Action<int, string> OnEventDrawSynced;
        public event Action<int, int> OnJournalIfGainSynced;
        public event Action<int, string, int> OnEventChoiceSynced;
        public event Action<int> OnTurnAdvancedSynced;
        public event Action<NetworkedGameState> OnGameStateSynced;

        public bool IsLocalHumanTurn(int currentPlayerIndex, PlayerData currentPlayer)
        {
            if (currentPlayer == null) return false;
            return currentPlayerIndex == LocalPlayerIndex && !currentPlayer.IsCpu;
        }

        public int RollDiceOnAuthority() =>
            RollRangeOnAuthority(GameConfig.MinDice, GameConfig.MaxDice);

        public int RollRangeOnAuthority(int minInclusive, int maxInclusive) =>
            _seededRandom != null
                ? _seededRandom.Next(minInclusive, maxInclusive + 1)
                : UnityEngine.Random.Range(minInclusive, maxInclusive + 1);

        public string DrawEventIdOnAuthority()
        {
            var ev = Sugoroku.Game.EventManager.Instance?.DrawEventOnAuthority();
            return ev?.EventId;
        }

        public void BroadcastDiceRoll(int playerIndex, int result) =>
            OnDiceRollSynced?.Invoke(playerIndex, result);

        public void BroadcastEventDraw(int playerIndex, string eventId) =>
            OnEventDrawSynced?.Invoke(playerIndex, eventId);

        public void BroadcastJournalIfGain(int playerIndex, int ifGain) =>
            OnJournalIfGainSynced?.Invoke(playerIndex, ifGain);

        public void BroadcastEventChoice(int playerIndex, string eventId, int choiceIndex) =>
            OnEventChoiceSynced?.Invoke(playerIndex, eventId, choiceIndex);

        public void BroadcastTurnAdvanced(int nextPlayerIndex) =>
            OnTurnAdvancedSynced?.Invoke(nextPlayerIndex);

        public NetworkedGameState CaptureState() =>
            NetworkedGameState.Capture(
                Sugoroku.Game.GameManager.Instance,
                Sugoroku.Game.TurnManager.Instance);

        public void ApplyState(NetworkedGameState state)
        {
            OnGameStateSynced?.Invoke(state);
        }
    }
}
