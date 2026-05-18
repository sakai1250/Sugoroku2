using UnityEditor;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Editor
{
    public static class PlayerStatRulesValidatorMenu
    {
        [MenuItem("Tools/Sugoroku/Validate stat rules (§7.2)")]
        public static void RunSelfTest()
        {
            var p = PlayerData.Create(0, CharacterType.Hobbyist, false);

            p.Money = -5;
            p.IfScore = -3;
            p.Mental = 999;
            p.MaxMental = 50;
            PlayerStatRules.Sanitize(p);

            bool ok = p.Money == 0 && p.IfScore == 0 && p.Mental == 50;
            ok &= PlayerStatRules.EvaluateDefeat(p) == GameOverReason.Bankruptcy;

            p.Money = 10;
            p.Mental = 0;
            ok &= PlayerStatRules.EvaluateDefeat(p) == GameOverReason.Missing;

            p.Mental = 30;
            p.IfScore = 0;
            ok &= PlayerStatRules.EvaluateDefeat(p) == GameOverReason.Expelled;

            if (ok)
                Debug.Log("§7.2 検証: クランプ・敗北判定 OK");
            else
                Debug.LogError("§7.2 検証: テスト失敗");
        }
    }
}
