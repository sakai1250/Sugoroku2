namespace Sugoroku.Data
{
    /// <summary>シーン間で受け渡すプレイ設定・結果。</summary>
    public static class GameSession
    {
        public static int           HumanCount     = 1;
        public static int           CpuCount       = 1;
        public static CharacterType HumanCharacter = CharacterType.Hobbyist;
        public static CharacterType[] HumanCharacters = { CharacterType.Hobbyist };

        public static GameOverReason LastGameOverReason = GameOverReason.None;
        public static PlayerSnapshot[] LastPlayers;

        public static int TotalPlayerCount => HumanCount + CpuCount;

        public static void EnsureHumanCharacters()
        {
            if (HumanCharacters != null && HumanCharacters.Length == HumanCount) return;

            var prev = HumanCharacters;
            HumanCharacters = new CharacterType[HumanCount];
            for (int i = 0; i < HumanCount; i++)
            {
                if (prev != null && i < prev.Length)
                    HumanCharacters[i] = prev[i];
                else
                    HumanCharacters[i] = HumanCharacter;
            }
        }

        public static CharacterType GetHumanCharacter(int humanSlot)
        {
            EnsureHumanCharacters();
            if (humanSlot < 0 || humanSlot >= HumanCharacters.Length)
                return HumanCharacter;
            return HumanCharacters[humanSlot];
        }

        public static void SetHumanCharacter(int humanSlot, CharacterType character)
        {
            EnsureHumanCharacters();
            if (humanSlot < 0 || humanSlot >= HumanCharacters.Length) return;
            HumanCharacters[humanSlot] = character;
            if (humanSlot == 0) HumanCharacter = character;
        }

        public static void SavePlayers(PlayerData[] players)
        {
            if (players == null) { LastPlayers = null; return; }
            LastPlayers = new PlayerSnapshot[players.Length];
            for (int i = 0; i < players.Length; i++)
                LastPlayers[i] = PlayerSnapshot.From(players[i]);
        }
    }

    [System.Serializable]
    public struct PlayerSnapshot
    {
        public string        Name;
        public CharacterType Character;
        public bool          IsCpu;
        public int           Money;
        public int           IfScore;
        public int           Mental;
        public int           MaxMental;
        public int           Virtue;
        public int           BoardPosition;
        public PlayerStatus  Status;
        public int           FinishRank;

        public static PlayerSnapshot From(PlayerData p) => new()
        {
            Name          = p.Name,
            Character     = p.Character,
            IsCpu         = p.IsCpu,
            Money         = p.Money,
            IfScore       = p.IfScore,
            Mental        = p.Mental,
            MaxMental     = p.MaxMental,
            Virtue        = p.Virtue,
            BoardPosition = p.BoardPosition,
            Status        = p.Status,
            FinishRank    = p.FinishRank
        };

        public int CalculateScore() => ScoreCalculator.Total(this);
    }
}
