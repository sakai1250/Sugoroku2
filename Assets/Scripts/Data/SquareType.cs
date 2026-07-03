namespace Sugoroku.Data
{
    public enum SquareType
    {
        Start,      // スタート
        Normal,     // 通常
        Event,      // イベント（意思決定）
        Tuition,    // 学費
        Journal,    // ジャーナル投稿（IF+）
        Lecture,    // 講義
        Rest,       // 休憩（メンタル回復）
        PartTime,   // バイト（所持金+）
        Bonus,      // チャンス（ランダム好効果）
        Penalty,    // ペナルティ（ランダム悪効果）
        Goal,       // ゴール
        Branch      // 分岐点（研究室ルート / バイトルート）
    }
}
