using System;

namespace Sugoroku.Data
{
    /// <summary>requirements §7.3 — 天才肌のジャーナル IF ボーナス（+1d6 pt、1回の加算上限）。</summary>
    public static class GeniusJournalRules
    {
        /// <summary>ベース IF + 天才肌／スキル分の 1d6 加算（倍率ではない）を合算し上限でクランプ。</summary>
        public static int ComputeIfGain(int baseIfGain, PlayerData player, Func<int, int, int> rollD6Inclusive)
        {
            if (player == null) return Math.Max(0, baseIfGain);

            int total = baseIfGain;

            if (player.Character == CharacterType.Genius)
                total += RollGeniusBonus(rollD6Inclusive);

            if (player.GeniusBonusActive)
            {
                total += RollGeniusBonus(rollD6Inclusive);
                player.GeniusBonusActive = false;
            }

            return Math.Min(total, GameConfig.MaxIfGainPerJournalSquare);
        }

        /// <summary>+1〜6 pt（1d6）。倍率補正は行わない。</summary>
        public static int RollGeniusBonus(Func<int, int, int> rollD6Inclusive)
        {
            rollD6Inclusive ??= DefaultRoll;
            return rollD6Inclusive(GameConfig.GeniusIfBonusMin, GameConfig.GeniusIfBonusMax);
        }

        private static int DefaultRoll(int min, int max) =>
            UnityEngine.Random.Range(min, max + 1);
    }
}
