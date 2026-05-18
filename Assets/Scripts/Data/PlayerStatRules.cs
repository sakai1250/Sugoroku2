namespace Sugoroku.Data
{
    /// <summary>requirements §7.2 — ステータス加算のクランプと敗北判定。</summary>
    public static class PlayerStatRules
    {
        public static int ClampMoney(int value) =>
            System.Math.Max(0, value);

        public static int ClampIfScore(int value) =>
            System.Math.Max(0, value);

        public static int ClampMental(int value, int maxMental)
        {
            int max = System.Math.Max(1, maxMental);
            return System.Math.Clamp(value, 0, max);
        }

        /// <summary>加算後の値を §7.2 に従って正規化（ネットワーク復元時など）。</summary>
        public static void Sanitize(PlayerData player)
        {
            if (player == null) return;
            player.MaxMental = System.Math.Max(1, player.MaxMental);
            player.Money     = ClampMoney(player.Money);
            player.IfScore   = ClampIfScore(player.IfScore);
            player.Mental    = ClampMental(player.Mental, player.MaxMental);
        }

        /// <summary>敗北条件: 所持金&lt;=0 / メンタル&lt;=0 / IF&lt;=0（いずれもクランプ後）。</summary>
        public static GameOverReason EvaluateDefeat(PlayerData player)
        {
            if (player == null || player.IsFinished) return GameOverReason.None;
            if (player.Money <= GameConfig.BankruptcyMoney) return GameOverReason.Bankruptcy;
            if (player.Mental <= GameConfig.MissingMental)  return GameOverReason.Missing;
            if (player.IfScore <= 0)                          return GameOverReason.Expelled;
            return GameOverReason.None;
        }
    }
}
