namespace Sugoroku.Data
{
    public enum AchievementId
    {
        Graduate,
        RankS,
        RankA,
        RankB,
        SurviveBankruptcyScare,
        ClearHobbyist,
        ClearSerious,
        ClearAthletic,
        ClearRich,
        ClearGenius,
        ClearAllCharacters,
        DailyChallengePlayed,
        BankruptcyDropout,
        MentalBreakdown
    }

    public readonly struct AchievementDefinition
    {
        public readonly AchievementId Id;
        public readonly string Title;
        public readonly string Description;

        public AchievementDefinition(AchievementId id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    public static class AchievementCatalog
    {
        public static readonly AchievementDefinition[] All =
        {
            new(AchievementId.Graduate, "修了達成", "1度でも修了する"),
            new(AchievementId.RankS, "教授エンド到達", "ランクS（教授エンド）で修了する"),
            new(AchievementId.RankA, "博士への道", "ランクAで修了する"),
            new(AchievementId.RankB, "無事に修了", "ランクBで修了する"),
            new(AchievementId.SurviveBankruptcyScare, "崖っぷちからの生還", "所持金10万円以下から立て直して修了する"),
            new(AchievementId.ClearHobbyist, "多趣味系で修了", "多趣味系キャラで修了する"),
            new(AchievementId.ClearSerious, "真面目系で修了", "真面目系キャラで修了する"),
            new(AchievementId.ClearAthletic, "体育会系で修了", "体育会系キャラで修了する"),
            new(AchievementId.ClearRich, "金持ち系で修了", "金持ち系キャラで修了する"),
            new(AchievementId.ClearGenius, "天才肌で修了", "天才肌キャラで修了する"),
            new(AchievementId.ClearAllCharacters, "全キャラ制覇", "全5キャラで修了する"),
            new(AchievementId.DailyChallengePlayed, "今日も一日お疲れ様", "デイリーチャレンジをプレイする"),
            new(AchievementId.BankruptcyDropout, "研究費が尽きた", "所持金が尽きて退学する"),
            new(AchievementId.MentalBreakdown, "燃え尽きました", "メンタルが尽きて失踪する"),
        };

        public static string GetTitle(AchievementId id)
        {
            foreach (var def in All)
                if (def.Id == id) return def.Title;
            return id.ToString();
        }

        public static string GetDescription(AchievementId id)
        {
            foreach (var def in All)
                if (def.Id == id) return def.Description;
            return "";
        }

        public static AchievementId? GetClearAchievement(CharacterType character) => character switch
        {
            CharacterType.Hobbyist => AchievementId.ClearHobbyist,
            CharacterType.Serious  => AchievementId.ClearSerious,
            CharacterType.Athletic => AchievementId.ClearAthletic,
            CharacterType.Rich     => AchievementId.ClearRich,
            CharacterType.Genius   => AchievementId.ClearGenius,
            _                      => null
        };
    }
}
