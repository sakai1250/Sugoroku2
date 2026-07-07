using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.Game
{
    /// <summary>盤面上の位置関係から生まれるプレイヤー間の相互作用(共同研究など)。</summary>
    public static class PlayerInteractionRules
    {
        /// <summary>マス種別のうち、同マス滞在で「共同研究」ボーナスが成立する対象。</summary>
        public static bool IsCollabEligible(SquareType type) =>
            type == SquareType.Journal || type == SquareType.Event || type == SquareType.Bonus;

        /// <summary>同マス滞在で「機材の取り合い」が成立する対象。</summary>
        public static bool IsEquipmentContestEligible(SquareType type) =>
            type == SquareType.Tuition || type == SquareType.Lecture || type == SquareType.Penalty;

        /// <summary>移動先マスに既にいる、他のアクティブなプレイヤーを1名探す。</summary>
        public static bool TryFindCoOccupant(PlayerData mover, PlayerData[] all, out PlayerData other)
        {
            other = null;
            if (mover == null || all == null) return false;

            foreach (var p in all)
            {
                if (p == null || p == mover) continue;
                if (p.IsFinished) continue;
                if (p.BoardPosition != mover.BoardPosition) continue;
                if (BranchRouteRules.IsInBranchRange(mover.BoardPosition) &&
                    p.ActiveBranch != mover.ActiveBranch)
                    continue;
                other = p;
                return true;
            }
            return false;
        }
    }
}
