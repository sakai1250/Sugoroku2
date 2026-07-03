using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>実績の解除状態を PlayerPrefs に永続化する。</summary>
    public static class AchievementStore
    {
        private const string KeyPrefix      = "ach_";
        private const string ClearKeyPrefix = "ach_clear_";

        public static event System.Action<AchievementId> OnUnlocked;

        public static bool IsUnlocked(AchievementId id) =>
            PlayerPrefs.GetInt(KeyPrefix + id, 0) != 0;

        /// <summary>初回解除時のみ true を返す(トースト表示等の判定に使える)。</summary>
        public static bool Unlock(AchievementId id)
        {
            if (IsUnlocked(id)) return false;
            PlayerPrefs.SetInt(KeyPrefix + id, 1);
            PlayerPrefs.Save();
            OnUnlocked?.Invoke(id);
            return true;
        }

        public static bool IsCharacterCleared(CharacterType character) =>
            PlayerPrefs.GetInt(ClearKeyPrefix + character, 0) != 0;

        /// <summary>キャラでの修了を記録し、必要なら個別実績+全キャラ制覇実績も解除する。</summary>
        public static void MarkCharacterCleared(CharacterType character)
        {
            PlayerPrefs.SetInt(ClearKeyPrefix + character, 1);
            PlayerPrefs.Save();

            var achievement = AchievementCatalog.GetClearAchievement(character);
            if (achievement.HasValue) Unlock(achievement.Value);

            bool allCleared = true;
            foreach (CharacterType c in System.Enum.GetValues(typeof(CharacterType)))
            {
                if (!IsCharacterCleared(c)) { allCleared = false; break; }
            }
            if (allCleared) Unlock(AchievementId.ClearAllCharacters);
        }

        public static int GetDailyBestScore(int dailySeed) =>
            PlayerPrefs.GetInt($"daily_best_{dailySeed}", int.MinValue);

        public static void SetDailyBestScore(int dailySeed, int score)
        {
            PlayerPrefs.SetInt($"daily_best_{dailySeed}", score);
            PlayerPrefs.Save();
        }
    }
}
