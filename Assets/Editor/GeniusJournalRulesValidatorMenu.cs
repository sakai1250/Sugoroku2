using UnityEditor;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Editor
{
    public static class GeniusJournalRulesValidatorMenu
    {
        [MenuItem("Tools/Sugoroku/Validate genius journal (§7.3)")]
        public static void RunSelfTest()
        {
            var player = PlayerData.Create(0, CharacterType.Genius, false);
            player.GeniusBonusActive = true;

            int maxRoll = 0;
            for (int i = 0; i < 200; i++)
            {
                var clone = PlayerData.Create(0, CharacterType.Genius, false);
                clone.GeniusBonusActive = true;
                int gain = GeniusJournalRules.ComputeIfGain(10, clone, (min, max) => max);
                if (gain > maxRoll) maxRoll = gain;
            }

            bool capOk = maxRoll <= GameConfig.MaxIfGainPerJournalSquare;
            int singleBonus = GeniusJournalRules.RollGeniusBonus((min, max) => 6);
            bool d6Ok = singleBonus == 6;

            if (capOk && d6Ok)
                Debug.Log($"§7.3 検証 OK（上限 {GameConfig.MaxIfGainPerJournalSquare}、1d6=+{singleBonus}）");
            else
                Debug.LogError($"§7.3 検証失敗: capOk={capOk} maxRoll={maxRoll}, d6Ok={d6Ok}");
        }
    }
}
