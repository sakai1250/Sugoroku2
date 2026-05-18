using UnityEngine;

namespace Sugoroku.Data
{
    public enum GameOverVisualStyle
    {
        BankruptcyNotice,
        MissingPhone,
        ExpulsionList
    }

    /// <summary>screen.md §5.1 / §5.2 — ゲームオーバー画面の文言と演出種別。</summary>
    public readonly struct GameOverOutcome
    {
        public readonly string              Title;
        public readonly string              Headline;
        public readonly string              Body;
        public readonly Color               AccentColor;
        public readonly Color               BackgroundTint;
        public readonly GameOverVisualStyle VisualStyle;

        public GameOverOutcome(
            string title, string headline, string body,
            Color accent, Color bgTint, GameOverVisualStyle visualStyle)
        {
            Title        = title;
            Headline     = headline;
            Body         = body;
            AccentColor  = accent;
            BackgroundTint = bgTint;
            VisualStyle  = visualStyle;
        }
    }

    public static class GameOverOutcomeResolver
    {
        private static readonly Color DarkBg = new(0.02f, 0.02f, 0.025f, 1f);

        public static GameOverOutcome Resolve(GameOverReason reason) => reason switch
        {
            GameOverReason.Bankruptcy => new GameOverOutcome(
                "【破産】学費・生活費未納による強制退学",
                "強制退学通知書が届いた",
                "学費・生活費の未納により、大学院から強制退学となりました。\n\n" +
                "研究室の机に、赤い「強制退学通知書」が叩きつけられた。\n" +
                "所持金を0以下にしないよう、バイトと節約のバランスが重要です。",
                new Color(0.85f, 0.15f, 0.12f, 0.95f),
                DarkBg,
                GameOverVisualStyle.BankruptcyNotice),

            GameOverReason.Missing => new GameOverOutcome(
                "【失踪】音信不通によるドロップアウト",
                "消息不明",
                "メンタルが0になり、研究室から音信不通となりました。\n\n" +
                "薄暗い部屋でスマートフォンの画面だけが虚しく光っている。\n" +
                "着信拒否、未読スルーの嵐——教授からの着信も、すべて無視されました。",
                new Color(0.55f, 0.65f, 0.95f, 0.9f),
                DarkBg,
                GameOverVisualStyle.MissingPhone),

            GameOverReason.Expelled => new GameOverOutcome(
                "【留年】業績不振による除籍",
                "除籍対象者一覧",
                "研究業績が不足し、業績不振により除籍となりました。\n\n" +
                "教授室の前に貼られた「除籍対象者一覧」に、自分の学籍番号が載っている。",
                new Color(0.75f, 0.55f, 0.2f, 0.95f),
                DarkBg,
                GameOverVisualStyle.ExpulsionList),

            _ => new GameOverOutcome(
                "ゲームオーバー",
                "研究生活の終わり",
                "研究生活はここで終わりました。",
                new Color(0.4f, 0.4f, 0.45f),
                DarkBg,
                GameOverVisualStyle.BankruptcyNotice)
        };
    }
}
