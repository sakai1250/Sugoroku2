using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>events.json の Tags に応じたカード配色。</summary>
    public static class EventTagColors
    {
        public static Color GetPanelColor(string[] tags)
        {
            var key = GetPrimaryTag(tags);
            return key switch
            {
                "トラブル" => new Color(0.42f, 0.14f, 0.16f, 0.94f),
                "緊急"     => new Color(0.45f, 0.22f, 0.10f, 0.94f),
                "学会"     => new Color(0.12f, 0.28f, 0.48f, 0.94f),
                "研究"     => new Color(0.14f, 0.26f, 0.42f, 0.94f),
                "生活"     => new Color(0.16f, 0.38f, 0.28f, 0.94f),
                "教授"     => new Color(0.32f, 0.18f, 0.42f, 0.94f),
                "後輩"     => new Color(0.20f, 0.34f, 0.40f, 0.94f),
                "ゼミ"     => new Color(0.18f, 0.30f, 0.36f, 0.94f),
                "事務"     => new Color(0.28f, 0.28f, 0.32f, 0.94f),
                "バイト"   => new Color(0.38f, 0.32f, 0.12f, 0.94f),
                _          => new Color(0.22f, 0.24f, 0.30f, 0.94f),
            };
        }

        public static Color GetBorderColor(string[] tags)
        {
            var c = GetPanelColor(tags);
            return new Color(
                Mathf.Min(c.r * 1.35f, 1f),
                Mathf.Min(c.g * 1.35f, 1f),
                Mathf.Min(c.b * 1.35f, 1f),
                1f);
        }

        public static Color GetSquareTypePanelColor(Sugoroku.Data.SquareType type) => type switch
        {
            Sugoroku.Data.SquareType.Start   => new Color(0.18f, 0.42f, 0.28f, 0.94f),
            Sugoroku.Data.SquareType.Goal    => new Color(0.48f, 0.38f, 0.10f, 0.94f),
            Sugoroku.Data.SquareType.Tuition => new Color(0.48f, 0.14f, 0.14f, 0.94f),
            Sugoroku.Data.SquareType.Journal => new Color(0.12f, 0.30f, 0.52f, 0.94f),
            Sugoroku.Data.SquareType.Lecture => new Color(0.14f, 0.36f, 0.44f, 0.94f),
            Sugoroku.Data.SquareType.Rest     => new Color(0.16f, 0.40f, 0.30f, 0.94f),
            Sugoroku.Data.SquareType.PartTime => new Color(0.38f, 0.32f, 0.12f, 0.94f),
            Sugoroku.Data.SquareType.Bonus    => new Color(0.42f, 0.34f, 0.10f, 0.94f),
            Sugoroku.Data.SquareType.Penalty => new Color(0.40f, 0.20f, 0.14f, 0.94f),
            _                              => new Color(0.24f, 0.26f, 0.32f, 0.92f),
        };

        public static string FormatTags(string[] tags)
        {
            if (tags == null || tags.Length == 0) return "";
            if (tags.Length == 1) return $"[{tags[0]}]";
            return $"[{tags[0]}/{tags[1]}]";
        }

        public static string GetPrimaryTag(string[] tags)
        {
            if (tags == null || tags.Length == 0) return "";
            foreach (var t in tags)
            {
                if (t is "トラブル" or "緊急") return t;
            }
            return tags[0];
        }
    }
}
