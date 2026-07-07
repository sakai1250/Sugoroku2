using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>
    /// 盤面分岐ルート(研究室/バイト)のインデックス定義。
    /// フォーク位置は <see cref="BoardLayoutGenerator"/> がマス数に応じて決定する。
    /// </summary>
    public static class BranchRouteRules
    {
        public const string ForkEventId = "E_BRANCH_FORK";

        public static int ForkIndex  => BoardLayoutGenerator.Current.ForkIndex;
        public static int RangeStart => BoardLayoutGenerator.Current.BranchRangeStart;
        public static int RangeEnd   => BoardLayoutGenerator.Current.BranchRangeEnd;
        public static int MergeLogicalIndex => BoardNavigation.MergeLogicalIndex;

        public static bool IsForkIndex(int index) => index == ForkIndex;

        public static bool IsInBranchRange(int index) => index >= RangeStart && index <= RangeEnd;
    }
}
