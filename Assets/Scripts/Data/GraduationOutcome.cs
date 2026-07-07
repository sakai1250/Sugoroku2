using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>screen.md §5.1 — 修了リザルトのランク別進路。</summary>
    public readonly struct GraduationOutcome
    {
        public readonly string Rank;
        public readonly string CareerPath;
        public readonly string Subtitle;
        public readonly Color  AccentColor;
        public readonly int    Score;

        public GraduationOutcome(string rank, string careerPath, string subtitle, Color accent, int score)
        {
            Rank        = rank;
            CareerPath  = careerPath;
            Subtitle    = subtitle;
            AccentColor = accent;
            Score       = score;
        }
    }

    public static class GraduationOutcomeResolver
    {
        public const int RankSThreshold = 800;
        public const int RankBThreshold = 300;
        public const int RankAThreshold = 500;
        public const int RankAIfMin     = 15;
        public const int RankAScoreMin  = 400;

        public static string ResolveRank(PlayerSnapshot player)
        {
            int score = ScoreCalculator.Total(player);
            int sThreshold = DifficultyRules.RankSThreshold(GameSession.Difficulty);
            if (score >= sThreshold) return "S";
            if (score >= RankAThreshold || (player.IfScore >= RankAIfMin && score >= RankAScoreMin))
                return "A";
            if (score >= RankBThreshold) return "B";
            return "C";
        }

        public static string ResolveRank(PlayerData player) =>
            ResolveRank(PlayerSnapshot.From(player));

        public static GraduationOutcome Resolve(PlayerSnapshot player)
        {
            int score = ScoreCalculator.Total(player);
            string rank = ResolveRank(player);
            return new GraduationOutcome(
                rank,
                ScoreCalculator.GetCareerForRank(rank),
                GetSubtitle(rank),
                GetAccentColor(rank),
                score);
        }

        public static string GetSubtitle(string rank) => rank switch
        {
            "S" => "学界の頂点——次はあなたがちゃぶ台返しする番",
            "A" => "学会より年次考課の方が現実的",
            "B" => "博士進学は『一旦保留』で社会へ",
            _   => "ポスドク修羅は遠い——実家の店を継ぐ"
        };

        public static Color GetAccentColor(string rank) => rank switch
        {
            "S" => new Color(1f, 0.85f, 0.25f),
            "A" => new Color(0.45f, 0.75f, 1f),
            "B" => new Color(0.5f, 0.9f, 0.55f),
            _   => new Color(0.65f, 0.6f, 0.75f)
        };

        public static string BuildPlayerReport(PlayerSnapshot player, int listIndex)
        {
            if (player.Status != PlayerStatus.Graduated)
            {
                return $"{listIndex + 1}位  {player.Name}  [{player.Character.DisplayName()}]\n" +
                       $"  脱落（修了に至らず）\n";
            }

            var outcome = Resolve(player);
            return
                $"{listIndex + 1}位  {player.Name}  [{player.Character.DisplayName()}]  ランク {outcome.Rank}\n" +
                $"  計算 {outcome.Score} pt  |  所持金 {player.Money}万 / IF {player.IfScore} / メンタル {player.Mental} / 徳 {player.Virtue}\n" +
                $"  → {outcome.CareerPath}\n" +
                $"     （{outcome.Subtitle}）\n";
        }
    }
}
