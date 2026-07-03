namespace Sugoroku.Board
{
    /// <summary>
    /// 盤面分岐ルート(研究室/バイト)のインデックス定義。
    /// フォークマス通過後、区間内は SquareType.Event のまま
    /// TurnManager が player.ActiveBranch に応じて効果を上書きする。
    /// </summary>
    public static class BranchRouteRules
    {
        public const string ForkEventId = "E_BRANCH_FORK";

        public const int ForkIndex  = 8;
        public const int RangeStart = 9;
        public const int RangeEnd   = 11;

        public static bool IsForkIndex(int index) => index == ForkIndex;

        public static bool IsInBranchRange(int index) => index >= RangeStart && index <= RangeEnd;
    }
}
