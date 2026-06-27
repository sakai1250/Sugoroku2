using TMPro;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>HUD 用の読みやすい TMP スタイル（大きめ・太字・アウトライン）。</summary>
    public static class HudTextStyle
    {
        public const float TextScale = 1.4f;
        public const float ResourceFontSize  = 19f * TextScale;
        public const float PlayerNameSize    = 22f * TextScale;
        public const float InfoFontSize      = 16f * TextScale;
        public const float LogFontSize       = 13f * TextScale;
        /// <summary>+15pt など駒上フロート表示。</summary>
        public const float JuiceFloatingFontSize = 34f * TextScale;
        /// <summary>移動中! / イベント! など状態コールアウト。</summary>
        public const float JuiceStatusFontSize   = 28f * TextScale;
        public const float OutlineWidth      = 0.14f;
        public const float JuiceOutlineWidth = 0.22f;
        public static readonly Color32 OutlineColor = new(0, 0, 0, 165);

        public static float Scale(float size) => size * TextScale;

        public static void ApplyReadable(TextMeshProUGUI tmp, float fontSize, Color color, bool bold = true)
        {
            if (tmp == null) return;
            JapaneseFontProvider.Apply(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = color;
            ApplyOutlineSafe(tmp, OutlineWidth, OutlineColor);
            tmp.raycastTarget = false;
        }

        public static void ApplyOutlineSafe(TextMeshProUGUI tmp, float width, Color color)
        {
            if (tmp == null) return;
            JapaneseFontProvider.Apply(tmp);
            if (tmp.font == null || tmp.font.material == null) return;

            try
            {
                tmp.outlineWidth = width;
                tmp.outlineColor = color;
            }
            catch (System.NullReferenceException)
            {
                // TMP のフォントマテリアル初期化前に呼ばれる場合があるため、アウトラインだけ諦める。
            }
        }

        public static void ApplyResource(TextMeshProUGUI tmp, Color color) =>
            ApplyReadable(tmp, ResourceFontSize, color, bold: true);

        public static void ApplyInfo(TextMeshProUGUI tmp, Color color) =>
            ApplyReadable(tmp, InfoFontSize, color, bold: false);

        public static void ApplyLog(TextMeshProUGUI tmp) =>
            ApplyReadable(tmp, LogFontSize, new Color(0.92f, 0.94f, 1f), bold: false);
    }
}
