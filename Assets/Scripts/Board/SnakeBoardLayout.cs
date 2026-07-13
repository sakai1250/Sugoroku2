using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// S字（スネーク）ルート。行ごとに左右へ折り返し、接続が一目で分かる。
    /// 論理マス数は可変、分岐点以降はフォークの行の下に確保した専用帯へ 2 レーンを配置。
    /// </summary>
    public static class SnakeBoardLayout
    {
        public static int CellCount => BoardLayoutGenerator.Current.CellCount;
        public static int PhysicalCellCount => BoardNavigation.PhysicalWaypointCount;

        // 参照画像に合わせて横長（1 行あたり 6〜8 マス程度）にする。
        public static int Columns => Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(CellCount * 1.9f)), 5, 8);
        public static int Rows    => Mathf.CeilToInt((float)CellCount / Columns);

        public static Vector3 GetWorldPosition(int index, float cellSpacing) =>
            GetWorldPosition(index, cellSpacing, cellSpacing);

        public static Vector3 GetWorldPosition(int index, float spacingX, float spacingY) =>
            GetGridWorldPosition(index, spacingX, spacingY);

        public static Vector3 GetGridWorldPosition(int index, float spacingX, float spacingY)
        {
            if (index < 0 || index >= CellCount) return Vector3.zero;

            int cols     = Columns;
            int row      = index / cols;
            int posInRow = index % cols;
            int col      = (row % 2 == 0) ? posInRow : (cols - 1 - posInRow);

            // 分岐レーン専用帯（BoardNavigation.BranchBandCount 行分）を
            // フォークの行の直後に確保するため、それより後ろの行を押し下げる。
            int forkRow    = BoardLayoutGenerator.Current.ForkIndex / cols;
            int pushedRows = row > forkRow ? BoardNavigation.BranchBandCount : 0;
            int rowY       = Rows - 1 - row - pushedRows;

            return new Vector3(col * spacingX, rowY * spacingY, 0f);
        }

        public static string GetDisplayName(int index)
        {
            if (index < 0 || index >= CellCount) return "";
            var data = BoardLayoutGenerator.Current;
            return index < data.DisplayNames.Length ? data.DisplayNames[index] ?? "" : "";
        }

        public static string GetDisplayLabel(int index) =>
            BoardNavigation.GetDisplayLabel(index, BranchRoute.None);

        public static string GetEventId(int index)
        {
            if (index < 0 || index >= CellCount) return "";
            var data = BoardLayoutGenerator.Current;
            return index < data.EventIds.Length ? data.EventIds[index] ?? "" : "";
        }

        public static SquareType GetSquareType(int index)
        {
            if (index < 0 || index >= CellCount) return SquareType.Normal;
            var data = BoardLayoutGenerator.Current;
            return index < data.SquareTypes.Length ? data.SquareTypes[index] : SquareType.Event;
        }
    }
}
