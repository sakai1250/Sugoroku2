namespace Sugoroku.Data
{
    /// <summary>シーン間で受け渡すプレイ設定・結果。</summary>
    public static class GameSession
    {
        const string PrefBoardCells   = "sugoroku_board_cells";
        const string PrefDifficulty   = "sugoroku_difficulty";

        public static int           HumanCount     = 1;
        public static int           CpuCount       = 1;
        public static CharacterType HumanCharacter = CharacterType.Hobbyist;
        public static CharacterType[] HumanCharacters = { CharacterType.Hobbyist };

        public static int            BoardCellCount = (int)BoardLengthOption.Standard;
        public static GameDifficulty Difficulty     = GameDifficulty.Normal;

        public static GameOverReason LastGameOverReason = GameOverReason.None;
        public static PlayerSnapshot[] LastPlayers;

        /// <summary>デイリーチャレンジ: 日付シード式の固定盤面でのスコアアタック。</summary>
        public static bool IsDailyChallenge;
        public static int  DailySeed;

        public static int TotalPlayerCount => HumanCount + CpuCount;

        public static void LoadSettings()
        {
            BoardCellCount = UnityEngine.PlayerPrefs.GetInt(PrefBoardCells, (int)BoardLengthOption.Standard);
            if (BoardCellCount != 16 && BoardCellCount != 20 && BoardCellCount != 24)
                BoardCellCount = (int)BoardLengthOption.Standard;

            int diff = UnityEngine.PlayerPrefs.GetInt(PrefDifficulty, (int)GameDifficulty.Normal);
            Difficulty = diff switch
            {
                (int)GameDifficulty.Easy   => GameDifficulty.Easy,
                (int)GameDifficulty.Hard   => GameDifficulty.Hard,
                _                          => GameDifficulty.Normal
            };
        }

        public static void SaveSettings()
        {
            UnityEngine.PlayerPrefs.SetInt(PrefBoardCells, BoardCellCount);
            UnityEngine.PlayerPrefs.SetInt(PrefDifficulty, (int)Difficulty);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void ApplyDailyChallengeDefaults()
        {
            BoardCellCount = (int)BoardLengthOption.Standard;
            Difficulty     = GameDifficulty.Normal;
        }

        public static int ComputeDailySeed(System.DateTime date) =>
            date.Year * 10000 + date.Month * 100 + date.Day;

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
        public bool          SurvivedBankruptcyScare;

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
            FinishRank    = p.FinishRank,
            SurvivedBankruptcyScare = p.SurvivedBankruptcyScare
        };

        public int CalculateScore() => ScoreCalculator.Total(this);
    }
}
