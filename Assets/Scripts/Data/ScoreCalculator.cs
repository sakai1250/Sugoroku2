using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sugoroku.Data
{
    public static class ScoreCalculator
    {
        public const string FormulaLine =
            "TotalScore = (IF * 100) + (所持金 * 0.5) + (徳 * 10) + (メンタル * 2)";

        public static int Total(PlayerData p) =>
            (int)(p.IfScore * GameConfig.ScoreWeightIf +
                   p.Money   * GameConfig.ScoreWeightMoney +
                   p.Virtue  * GameConfig.ScoreWeightVirtue +
                   p.Mental  * GameConfig.ScoreWeightMental);

        public static int Total(PlayerSnapshot p) =>
            (int)(p.IfScore * GameConfig.ScoreWeightIf +
                   p.Money   * GameConfig.ScoreWeightMoney +
                   p.Virtue  * GameConfig.ScoreWeightVirtue +
                   p.Mental  * GameConfig.ScoreWeightMental);

        public static int GetProvisionalRank(PlayerData player, IEnumerable<PlayerData> allPlayers)
        {
            if (player == null) return 0;
            var active = allPlayers?
                .Where(p => p != null && !p.IsEliminated)
                .OrderByDescending(Total)
                .ThenBy(p => p.Index)
                .ToList() ?? new List<PlayerData>();

            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Index == player.Index)
                    return i + 1;
            }
            return 0;
        }

        /// <summary>screen.md §3.3 — ポーズ画面用の計算式・内訳・暫定順位。</summary>
        public static string BuildPauseStatusReport(PlayerData player, IEnumerable<PlayerData> allPlayers)
        {
            if (player == null) return "";

            int ifPart     = (int)(player.IfScore * GameConfig.ScoreWeightIf);
            int moneyPart  = (int)(player.Money   * GameConfig.ScoreWeightMoney);
            int virtuePart = (int)(player.Virtue  * GameConfig.ScoreWeightVirtue);
            int mentalPart = (int)(player.Mental  * GameConfig.ScoreWeightMental);
            int total      = Total(player);
            int rank       = GetProvisionalRank(player, allPlayers);
            int playerCount = allPlayers?.Count(p => p != null && !p.IsEliminated) ?? 1;

            var sb = new StringBuilder();
            sb.AppendLine($"【{player.Name}（{player.Character.DisplayName()}）】");
            if (rank > 0)
                sb.AppendLine($"暫定順位: {rank}位 / {playerCount}人");
            sb.AppendLine();
            sb.AppendLine("【現在の総合スコア計算】");
            sb.AppendLine(FormulaLine);
            sb.AppendLine();
            sb.AppendLine($"  IF {player.IfScore:F1} pt × {GameConfig.ScoreWeightIf:0.#} = {ifPart}");
            sb.AppendLine($"  所持金 {player.Money} 万円 × {GameConfig.ScoreWeightMoney:0.#} = {moneyPart}");
            sb.AppendLine($"  徳 {player.Virtue} pt × {GameConfig.ScoreWeightVirtue:0.#} = {virtuePart}");
            sb.AppendLine($"  メンタル {player.Mental} × {GameConfig.ScoreWeightMental:0.#} = {mentalPart}");
            sb.AppendLine();
            sb.AppendLine($"現在のスコア: {total} pt");

            var ranking = BuildProvisionalRanking(allPlayers);
            if (!string.IsNullOrEmpty(ranking))
            {
                sb.AppendLine();
                sb.Append(ranking);
            }

            return sb.ToString().TrimEnd();
        }

        public static string BuildBreakdown(PlayerData p) =>
            BuildPauseStatusReport(p, new[] { p });

        private static string BuildProvisionalRanking(IEnumerable<PlayerData> allPlayers)
        {
            var list = allPlayers?
                .Where(p => p != null && !p.IsEliminated)
                .OrderByDescending(Total)
                .ThenBy(p => p.Index)
                .ToList();

            if (list == null || list.Count <= 1) return "";

            var sb = new StringBuilder();
            sb.AppendLine("【全員の暫定スコア】");
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                sb.AppendLine($"  {i + 1}位  {p.Name}  …  {Total(p)} pt");
            }
            return sb.ToString().TrimEnd();
        }

        public static string GetGraduationRank(int score) => score switch
        {
            >= 800 => "S",
            >= 500 => "A",
            >= 300 => "B",
            _      => "C"
        };

        public static string GetCareerForRank(string rank) => rank switch
        {
            "S" => "教授",
            "A" => "大手メーカー研究職",
            "B" => "修士就職",
            _   => "実家稼業"
        };
    }
}
