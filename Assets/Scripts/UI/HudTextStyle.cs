using TMPro;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>HUD 用の読みやすい TMP スタイル（大きめ・太字・アウトライン）。</summary>
    public static class HudTextStyle
    {
        public const float ResourceFontSize  = 24f;
        public const float PlayerNameSize    = 28f;
        public const float InfoFontSize      = 20f;
        public const float LogFontSize       = 18f;
        public const float OutlineWidth      = 0.22f;
        public static readonly Color32 OutlineColor = new(0, 0, 0, 200);

        public static void ApplyReadable(TextMeshProUGUI tmp, float fontSize, Color color, bool bold = true)
        {
            if (tmp == null) return;
            JapaneseFontProvider.Apply(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = color;
            tmp.outlineWidth = OutlineWidth;
            tmp.outlineColor = OutlineColor;
            tmp.raycastTarget = false;
        }

        public static void ApplyResource(TextMeshProUGUI tmp, Color color) =>
            ApplyReadable(tmp, ResourceFontSize, color, bold: true);

        public static void ApplyInfo(TextMeshProUGUI tmp, Color color) =>
            ApplyReadable(tmp, InfoFontSize, color, bold: false);

        public static void ApplyLog(TextMeshProUGUI tmp) =>
            ApplyReadable(tmp, LogFontSize, new Color(0.92f, 0.94f, 1f), bold: false);
    }
}
