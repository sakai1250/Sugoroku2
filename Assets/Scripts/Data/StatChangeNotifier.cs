namespace Sugoroku.Data
{
    /// <summary>ステータス増減の通知（HUD発光・フローティングテキスト用）。</summary>
    public static class StatChangeNotifier
    {
        public static event System.Action<PlayerData, int, int, int, int> OnChanged;

        public static void Notify(PlayerData player, int money, int ifScore, int mental, int virtue)
        {
            if (money == 0 && ifScore == 0 && mental == 0 && virtue == 0) return;
            OnChanged?.Invoke(player, money, ifScore, mental, virtue);
        }
    }
}
