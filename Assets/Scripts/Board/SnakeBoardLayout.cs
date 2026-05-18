using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// 大学院2年・20マス固定の S字（スネーク）ルート。左上スタート → 右折返し × 4行。
    /// 配列インデックス 0 = スタート、19 = ゴール（進行順＝Unity 配列順）。
    /// </summary>
    public static class SnakeBoardLayout
    {
        public const int CellCount = 20;
        public const int Columns   = 5;
        public const int Rows      = 4;

        /// <summary>イベントマスに固定配置する EventId（events.json）。</summary>
        private static readonly string[] FixedEventIds =
        {
            "", "", "", "", "",
            "", "", "", "", "E012",
            "E014", "", "", "", "",
            "", "", "E016", "", "",
        };

        public static Vector3 GetWorldPosition(int index, float cellSpacing)
        {
            if (index < 0 || index >= CellCount) return Vector3.zero;

            int row      = index / Columns;
            int posInRow = index % Columns;
            int col      = (row % 2 == 0) ? posInRow : (Columns - 1 - posInRow);
            int rowY     = Rows - 1 - row;

            return new Vector3(col * cellSpacing, rowY * cellSpacing, 0f);
        }

        public static string GetDisplayName(int index)
        {
            if (index < 0 || index >= CellCount) return "";
            return index switch
            {
                0  => "研究室配属",
                2  => "ゼミ発表",
                3  => "バイト",
                4  => "学費納入",
                7  => "ジャーナル",
                11 => "学費納入",
                14 => "バイト",
                15 => "ジャーナル",
                18 => "修論提出",
                19 => "修了判定",
                _  => ""
            };
        }

        public static string GetDisplayLabel(int index)
        {
            var named = GetDisplayName(index);
            if (!string.IsNullOrEmpty(named)) return named;
            var type = GetSquareType(index);
            var shortTag = Waypoint.GetTypeShortLabel(type);
            return string.IsNullOrEmpty(shortTag) ? $"マス{index + 1}" : $"{shortTag}・マス{index + 1}";
        }

        public static string GetEventId(int index)
        {
            if (index < 0 || index >= CellCount) return "";
            return FixedEventIds[index];
        }

        public static SquareType GetSquareType(int index)
        {
            if (index == 0) return SquareType.Start;
            if (index == CellCount - 1) return SquareType.Goal;

            return index switch
            {
                2       => SquareType.Lecture,
                3 or 14 => SquareType.PartTime,
                4 or 11 => SquareType.Tuition,
                5 or 13 => SquareType.Rest,
                7 or 15 => SquareType.Journal,
                9 or 10 or 17 => SquareType.Event,
                18      => SquareType.Journal,
                _       => SquareType.Normal,
            };
        }
    }
}
