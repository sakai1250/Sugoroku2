namespace Sugoroku.Data
{
    /// <summary>難易度ごとのゲームバランス補正。</summary>
    public static class DifficultyRules
    {
        public static int TuitionCost(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 15,
            GameDifficulty.Hard   => 28,
            _                     => GameConfig.BaseTuitionCost
        };

        public static int InitialMoneyBonus(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 10,
            GameDifficulty.Hard   => -8,
            _                     => 0
        };

        public static int InitialMentalBonus(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 10,
            GameDifficulty.Hard   => -10,
            _                     => 0
        };

        public static int MaxMentalBonus(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 5,
            GameDifficulty.Hard   => -5,
            _                     => 0
        };

        public static int RankSThreshold(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 850,
            GameDifficulty.Hard   => 720,
            _                     => GraduationOutcomeResolver.RankSThreshold
        };

        public static float CpuChoiceNoise(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => 0.35f,
            GameDifficulty.Hard   => 0f,
            _                     => 0.15f
        };

        public static string GetLabel(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy   => "やさしい",
            GameDifficulty.Hard   => "むずかしい",
            _                     => "ふつう"
        };

        public static string GetBoardLengthLabel(int cellCount) => cellCount switch
        {
            40 => "ロングコース版（40マス）",
            16 => "短め（16マス）",
            24 => "長め（24マス）",
            28 => "標準（28マス）",
            _  => $"標準（{cellCount}マス）"
        };
    }
}
