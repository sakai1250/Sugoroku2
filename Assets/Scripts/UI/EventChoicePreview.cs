using System.Collections.Generic;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §4.2 — 選択肢のステータス変動プレビュー文言。</summary>
    public static class EventChoicePreview
    {
        private const string PosMoney  = "#66FF99";
        private const string NegMoney  = "#FF6666";
        private const string PosIf     = "#66CCFF";
        private const string NegIf     = "#FF6666";
        private const string PosMental = "#66FF99";
        private const string NegMental = "#FF6666";
        private const string PosVirtue = "#CC99FF";
        private const string NegVirtue = "#FF8888";

        public static string FormatRich(EventChoice c)
        {
            var parts = new List<string>();
            if (c.MoneyChange != 0)
                parts.Add(Delta("所持金", c.MoneyChange, "万", PosMoney, NegMoney));
            if (c.MentalChange != 0)
                parts.Add(Delta("メンタル", c.MentalChange, "", PosMental, NegMental));
            if (c.IfScoreChange != 0)
                parts.Add(Delta("IF", c.IfScoreChange, "", PosIf, NegIf));
            if (c.VirtueChange != 0)
                parts.Add(Delta("徳", c.VirtueChange, "", PosVirtue, NegVirtue));

            return parts.Count > 0
                ? "（" + string.Join(" / ", parts) + "）"
                : "（変化なし）";
        }

        public static string FormatFailBadge(string failReason)
        {
            if (string.IsNullOrEmpty(failReason)) return "";
            if (failReason.StartsWith("[")) return failReason;
            return $"[{failReason}]";
        }

        private static string Delta(string name, int v, string unit, string pos, string neg)
        {
            string sign = v > 0 ? "+" : "";
            string color = v > 0 ? pos : neg;
            return $"<color={color}>{name} {sign}{v}{unit}</color>";
        }
    }
}
