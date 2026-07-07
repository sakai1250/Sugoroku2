using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// <see cref="SnakeBoardLayout"/> への互換エイリアス（旧 hex レイアウト廃止）。
    /// </summary>
    public static class LayeredBoardLayout
    {
        public static int CellCount => SnakeBoardLayout.CellCount;

        public static Vector3 GetWorldPosition(int index, float tileWorldWidth) =>
            SnakeBoardLayout.GetWorldPosition(index, tileWorldWidth);

        public static SquareType GetSquareType(int index) =>
            SnakeBoardLayout.GetSquareType(index);
    }
}
