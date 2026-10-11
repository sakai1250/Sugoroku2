namespace Sugoroku.Data
{
    /// <summary>
    /// ゲームの3大ステージ（シーズン）定義。
    /// </summary>
    public enum SeasonType
    {
        /// <summary>Stage 1: 研究執筆シーズン（研究室・論文執筆・ゼミ発表）</summary>
        ResearchWriting = 0,

        /// <summary>Stage 2: 学会シーズン（学会ホール・ポスターセッション・渡航）</summary>
        Conference = 1,

        /// <summary>Stage 3: バイトシーズン（繁華街・居酒屋・生活費確保・修了審査）</summary>
        PartTime = 2
    }
}
