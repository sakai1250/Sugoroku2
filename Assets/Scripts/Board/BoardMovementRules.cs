namespace Sugoroku.Board
{
    /// <summary>駒の進行ルール（強制停止マスなど）。</summary>
    public static class BoardMovementRules
    {
        /// <summary>通過時に必ず止まるマス（残り歩数があってもここで停止）。</summary>
        public static bool IsMandatoryStop(int index) =>
            BranchRouteRules.IsForkIndex(index);

        /// <summary>ダイス目を進めたときの実際の着地インデックス（強制停止・ゴールを考慮）。</summary>
        public static int ResolveLandingIndex(int fromIndex, int steps, int boardSize)
        {
            if (steps <= 0 || boardSize <= 0) return fromIndex;

            int pos = fromIndex;
            int last = boardSize - 1;

            for (int i = 0; i < steps; i++)
            {
                pos++;
                if (pos >= last) return last;
                if (IsMandatoryStop(pos)) return pos;
            }

            return pos;
        }
    }
}
