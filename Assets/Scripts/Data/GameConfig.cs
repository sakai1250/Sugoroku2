namespace Sugoroku.Data
{
    public static class GameConfig
    {
        public const int BoardSize        = 20;
        public const int InitialMoney     = 45;
        public const int InitialIfScore   = 0;
        public const int InitialMental    = 75;
        public const int MaxMental        = 75;
        public const int InitialVirtue    = 0;
        public const int TuitionCost      = 20;
        public const int MinDice          = 1;
        public const int MaxDice          = 6;
        public const int MaxPlayers       = 4;

        // ゲームオーバー判定
        public const int BankruptcyMoney  = 0;
        public const int MissingMental    = 0;

        // スコア計算: TotalScore = (IF*100) + (所持金*0.5) + (徳*10) + (メンタル*2)
        public const float ScoreWeightIf      = 100f;
        public const float ScoreWeightMoney   = 0.5f;
        public const float ScoreWeightVirtue  = 10f;
        public const float ScoreWeightMental  = 2f;

        // 天才肌: ジャーナル等での IF 加算は「1d6 ボーナス pt」（倍率ではない）
        public const int GeniusIfBonusMin           = 1;
        public const int GeniusIfBonusMax           = 6;
        public const int MaxIfGainPerJournalSquare  = 22;

        // 徳による救済（全選択肢が条件未達のとき）
        public const int VirtueRescueThreshold = 20;

        // スキル使用コスト
        public const int RichSkillCost        = 10;

        // アニメーション時間
        public const float AnimationSpeed = 0.7f;
        public const float AnimationDurationScale = 1f / AnimationSpeed;
        public const float DiceRollDuration   = 1.0f;
        public const float PieceMoveDuration  = 0.25f;
        public const float FloatingTextDuration = 0.6f;
        public const float StatFloatGapSeconds  = 0.3f;
        public const float HudStatFlashDuration = 1.5f;
        /// <summary>events.json から算出。1回のイベントで破産・失踪し得る所持金・メンタルの上限。</summary>
        public const int PinchMoneyThresholdFallback  = 40;
        public const int PinchMentalThresholdFallback = 40;
        public const float CameraDiceLookaheadBlend = 0.42f;
        public const float CameraEventZoomScale = 0.72f;

        public static float AnimationDuration(float seconds) => seconds * AnimationDurationScale;
    }
}
