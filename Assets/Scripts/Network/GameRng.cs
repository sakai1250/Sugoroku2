using UnityEngine;

namespace Sugoroku.Network
{
    /// <summary>§7.3 — ゲーム内乱数は StateAuthority 経由（オンライン時）。</summary>
    public static class GameRng
    {
        public static int Range(int minInclusive, int maxInclusive)
        {
            var host = NetworkSessionHost.Instance;
            if (host != null)
                return host.RollRange(minInclusive, maxInclusive);
            return UnityEngine.Random.Range(minInclusive, maxInclusive + 1);
        }

        public static float Value01() => Range(0, 9999) / 10000f;
    }
}
