using System;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.Network
{
    /// <summary>
    /// 再接続用の盤面スナップショット（requirements §6.1 Networked 変数のローカル表現）。
    /// Fusion 導入時は [Networked] プロパティへ写経する。
    /// </summary>
    [Serializable]
    public struct NetworkedPlayerState
    {
        public int           Index;
        public string        Name;
        public CharacterType Character;
        public bool          IsCpu;
        public int           Money;
        public int           IfScore;
        public int           Mental;
        public int           MaxMental;
        public int           Virtue;
        public int           BoardPosition;
        public int           SkipTurns;
        public int           IgnoreNextEvents;
        public bool          SkillUsedThisTurn;
        public bool          GeniusBonusActive;
        public bool          AthleticMentalImmunity;
        public PlayerStatus  Status;
        public int           FinishRank;

        public static NetworkedPlayerState From(PlayerData p) => new()
        {
            Index                 = p.Index,
            Name                  = p.Name,
            Character             = p.Character,
            IsCpu                 = p.IsCpu,
            Money                 = p.Money,
            IfScore               = p.IfScore,
            Mental                = p.Mental,
            MaxMental             = p.MaxMental,
            Virtue                = p.Virtue,
            BoardPosition         = p.BoardPosition,
            SkipTurns             = p.SkipTurns,
            IgnoreNextEvents      = p.IgnoreNextEvents,
            SkillUsedThisTurn     = p.SkillUsedThisTurn,
            GeniusBonusActive     = p.GeniusBonusActive,
            AthleticMentalImmunity= p.AthleticMentalImmunity,
            Status                = p.Status,
            FinishRank            = p.FinishRank,
        };

        public void ApplyTo(PlayerData p)
        {
            p.Name                  = Name;
            p.Character             = Character;
            p.IsCpu                 = IsCpu;
            p.Money                 = Money;
            p.IfScore               = IfScore;
            p.Mental                = Mental;
            p.MaxMental             = MaxMental;
            p.Virtue                = Virtue;
            p.BoardPosition         = BoardPosition;
            p.SkipTurns             = SkipTurns;
            p.IgnoreNextEvents      = IgnoreNextEvents;
            p.SkillUsedThisTurn     = SkillUsedThisTurn;
            p.GeniusBonusActive     = GeniusBonusActive;
            p.AthleticMentalImmunity= AthleticMentalImmunity;
            p.Status                = Status;
            p.FinishRank            = FinishRank;
            PlayerStatRules.Sanitize(p);
        }
    }

    [Serializable]
    public struct NetworkedGameState
    {
        public int                    CurrentPlayerIndex;
        public TurnState              TurnState;
        public NetworkedPlayerState[] Players;

        public static NetworkedGameState Capture(GameManager gm, TurnManager turn)
        {
            if (gm == null || !gm.IsInitialized)
                return default;

            var players = gm.GetAllPlayers();
            var snap = new NetworkedPlayerState[players.Length];
            for (int i = 0; i < players.Length; i++)
                snap[i] = NetworkedPlayerState.From(players[i]);

            return new NetworkedGameState
            {
                CurrentPlayerIndex = gm.GetCurrentPlayerIndex(),
                TurnState          = turn != null ? turn.CurrentState : TurnState.WaitAction,
                Players            = snap,
            };
        }
    }
}
