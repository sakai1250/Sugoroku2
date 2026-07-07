using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// S字（スネーク）ルート。論理マス数は可変、分岐点以降は上下レーンに物理分岐。
    /// </summary>
    public static class SnakeBoardLayout
    {
        public static int CellCount => BoardLayoutGenerator.Current.CellCount;
        public static int PhysicalCellCount => BoardNavigation.PhysicalWaypointCount;
        public static int Columns   => BoardLayoutGenerator.Current.Columns;
        public static int Rows      => BoardLayoutGenerator.Current.Rows;

        public static Vector3 GetWorldPosition(int index, float cellSpacing) =>
            GetWorldPosition(index, cellSpacing, cellSpacing);

        public static Vector3 GetWorldPosition(int index, float spacingX, float spacingY) =>
            GetGridWorldPosition(index, spacingX, spacingY);

        public static Vector3 GetGridWorldPosition(int index, float spacingX, float spacingY)
        {
            if (index < 0 || index >= CellCount) return Vector3.zero;

            int row      = index / Columns;
            int posInRow = index % Columns;
            int col      = (row % 2 == 0) ? posInRow : (Columns - 1 - posInRow);
            int rowY     = Rows - 1 - row;

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
