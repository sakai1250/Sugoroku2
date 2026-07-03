namespace Sugoroku.Data
{
    /// <summary>ゲーム結果から実績解除を判定する。</summary>
    public static class AchievementEvaluator
    {
        /// <summary>所持金がこの値以下まで落ちた場合「崖っぷちからの生還」の対象とする。</summary>
        public const int BankruptcyScareThreshold = 10;

        public static void OnGraduated(PlayerSnapshot player, string rank)
        {
            AchievementStore.Unlock(AchievementId.Graduate);

            switch (rank)
            {
                case "S": AchievementStore.Unlock(AchievementId.RankS); break;
                case "A": AchievementStore.Unlock(AchievementId.RankA); break;
                case "B": AchievementStore.Unlock(AchievementId.RankB); break;
            }

            if (player.SurvivedBankruptcyScare)
                AchievementStore.Unlock(AchievementId.SurviveBankruptcyScare);

            AchievementStore.MarkCharacterCleared(player.Character);
        }

        /// <summary>デイリーチャレンジをプレイしたことを記録する(結果に関わらず解除)。</summary>
        public static void OnDailyChallengePlayed() =>
            AchievementStore.Unlock(AchievementId.DailyChallengePlayed);

        /// <summary>破産/失踪等での脱落を「大学院生あるある」実績として解除する。</summary>
        public static void OnGameOver(GameOverReason reason)
        {
            switch (reason)
            {
                case GameOverReason.Bankruptcy:
                    AchievementStore.Unlock(AchievementId.BankruptcyDropout);
                    break;
                case GameOverReason.Missing:
                    AchievementStore.Unlock(AchievementId.MentalBreakdown);
                    break;
            }
        }
    }
}
