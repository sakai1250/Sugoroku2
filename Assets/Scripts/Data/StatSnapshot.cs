namespace Sugoroku.Data
{
    /// <summary>リザルト画面の推移グラフ用、1ターン時点のスナップショット。</summary>
    [System.Serializable]
    public struct StatSnapshot
    {
        public int Turn;
        public int IfScore;
        public int Mental;

        public StatSnapshot(int turn, int ifScore, int mental)
        {
            Turn = turn;
            IfScore = ifScore;
            Mental = mental;
        }
    }
}
