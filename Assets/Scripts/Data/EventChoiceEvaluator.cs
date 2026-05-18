using System;
using System.Text.RegularExpressions;

namespace Sugoroku.Data
{
    public static class EventChoiceEvaluator
    {
        private static readonly Regex ConditionPattern = new(
            @"^\s*(Money|IfScore|Mental|Virtue)\s*(>=|<=|==|>|<)\s*(-?\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool MeetsConditions(EventChoice choice, PlayerData player, out string failReason)
        {
            return MeetsConditions(choice, player, ignoreConditions: false, out failReason);
        }

        public static bool MeetsConditions(EventChoice choice, PlayerData player, bool ignoreConditions, out string failReason)
        {
            failReason = null;
            if (ignoreConditions) return true;
            if (choice == null || string.IsNullOrWhiteSpace(choice.Conditions))
                return true;

            if (!TryParse(choice.Conditions, out var stat, out var op, out var required))
            {
                UnityEngine.Debug.LogWarning($"未対応の conditions: {choice.Conditions}");
                return true;
            }

            int current = GetStat(player, stat);
            if (Compare(current, op, required)) return true;

            failReason = stat switch
            {
                "Money"  => $"[所持金不足: あと{Math.Max(0, required - current)}万円]",
                "IfScore"=> $"[IF不足: あと{Math.Max(0, required - current)} pt]",
                "Mental" => $"[メンタル不足: あと{Math.Max(0, required - current)}]",
                "Virtue" => $"[徳不足: あと{Math.Max(0, required - current)} pt]",
                _        => "[条件未達]"
            };
            return false;
        }

        private static bool TryParse(string raw, out string stat, out string op, out int required)
        {
            stat = op = null;
            required = 0;
            var m = ConditionPattern.Match(raw);
            if (!m.Success) return false;
            stat     = m.Groups[1].Value;
            op       = m.Groups[2].Value;
            required = int.Parse(m.Groups[3].Value);
            return true;
        }

        private static int GetStat(PlayerData p, string stat) => stat switch
        {
            "Money"   => p.Money,
            "IfScore" => p.IfScore,
            "Mental"  => p.Mental,
            "Virtue"  => p.Virtue,
            _         => 0
        };

        private static bool Compare(int current, string op, int required) => op switch
        {
            ">=" => current >= required,
            "<=" => current <= required,
            ">"  => current > required,
            "<"  => current < required,
            "==" => current == required,
            _    => true
        };
    }
}
