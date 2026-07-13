using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>バナー層の状況分類（画面上部の帯に出す告知の種類）。</summary>
    public enum BannerSituation
    {
        TurnStart,
        RollPrompt,
        Moving,
        MassCheck,
        GoodEffect,
        BadEffect,
        EventLabel,
        Generic
    }

    /// <summary>イベントの種類・状況からカットインの入場スタイルを決める（同種は同じ入場で一貫）。</summary>
    public static class CutInStyleMap
    {
        /// <summary>中央カード層: イベントのタグ・カテゴリから入場スタイルを選ぶ。</summary>
        public static CutInStyle PickEvent(EventMaster ev)
        {
            var tags = ev?.Tags;
            if (HasTag(tags, "トラブル")) return CutInStyle.BounceDown;
            if (HasTag(tags, "緊急"))     return CutInStyle.SlideDown;
            if (HasTag(tags, "進路") || HasTag(tags, "分岐"))   return CutInStyle.ZoomIn;
            if (HasTag(tags, "学会") || HasTag(tags, "研究"))   return CutInStyle.SlideUp;
            if (HasTag(tags, "ゼミ") || HasTag(tags, "教授") || HasTag(tags, "講義")) return CutInStyle.SlideLeft;
            if (HasTag(tags, "バイト") || HasTag(tags, "生活") || HasTag(tags, "事務")) return CutInStyle.SlideRight;
            if (HasTag(tags, "後輩")) return CutInStyle.FlipIn;
            if (HasTag(tags, "金運") || HasTag(tags, "経済")) return CutInStyle.Spin;
            if (HasTag(tags, "恋愛") || HasTag(tags, "幸運")) return CutInStyle.FloatIn;

            return EventMasuArt.ResolveCategory(SquareType.Event, tags) switch
            {
                EventMasuArt.Category.Research => CutInStyle.SlideUp,
                EventMasuArt.Category.Lab      => CutInStyle.SlideLeft,
                EventMasuArt.Category.Economy  => CutInStyle.SlideRight,
                _                              => CutInStyle.CenterPop
            };
        }

        /// <summary>バナー層: 状況ごとの軽量な入場スタイル。</summary>
        public static CutInStyle Banner(BannerSituation s) => s switch
        {
            BannerSituation.TurnStart  => CutInStyle.SlideLeft,
            BannerSituation.RollPrompt => CutInStyle.FloatIn,
            BannerSituation.Moving     => CutInStyle.SlideRight,
            BannerSituation.MassCheck  => CutInStyle.Fade,
            BannerSituation.GoodEffect => CutInStyle.SlideUp,
            BannerSituation.BadEffect  => CutInStyle.SlideDown,
            BannerSituation.EventLabel => CutInStyle.CenterPop,
            _                          => CutInStyle.Fade,
        };

        /// <summary>入場のインパクト（カメラシェイク強度・時間）。</summary>
        public static (float amplitude, float duration) Impact(CutInStyle s) => s switch
        {
            CutInStyle.BounceDown => (0.12f, 0.22f),
            CutInStyle.SlideDown  => (0.09f, 0.18f),
            CutInStyle.ZoomIn     => (0.10f, 0.18f),
            CutInStyle.Spin       => (0.10f, 0.20f),
            _                     => (0.05f, 0.14f),
        };

        private static bool HasTag(string[] tags, string tag)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
                if (tags[i] == tag) return true;
            return false;
        }
    }
}
