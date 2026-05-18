namespace Sugoroku.Data
{
    public static class SquareEffectLabels
    {
        public static string Get(SquareType type) => type switch
        {
            SquareType.Start   => "スタート",
            SquareType.Normal  => "通常",
            SquareType.Event   => "イベント",
            SquareType.Tuition => "学費",
            SquareType.Journal => "論文投稿",
            SquareType.Lecture => "ゼミ講義",
            SquareType.Rest    => "休息",
            SquareType.PartTime=> "バイト",
            SquareType.Bonus   => "チャンス",
            SquareType.Penalty => "ペナルティ",
            SquareType.Goal    => "ゴール",
            _                  => type.ToString()
        };
    }
}
