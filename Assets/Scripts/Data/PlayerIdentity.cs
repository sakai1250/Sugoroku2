using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>同一キャラの区別（1P/2P…）とスロット識別色。</summary>
    public static class PlayerIdentity
    {
        public static readonly Color[] SlotColors =
        {
            new Color(0.95f, 0.32f, 0.32f), // 1P: 赤
            new Color(0.28f, 0.62f, 0.98f), // 2P: 青
            new Color(0.32f, 0.82f, 0.42f), // 3P: 緑
            new Color(0.98f, 0.78f, 0.22f), // 4P: 黄
        };

        public static void Apply(PlayerData[] players)
        {
            if (players == null || players.Length == 0) return;

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                p.Name = $"{p.Index + 1}P";
                p.PieceTint = SlotColors[i % SlotColors.Length];
            }
        }

        public static string FormatHudLabel(PlayerData player)
        {
            if (player == null) return "";

            var typeLabel = player.Character.DisplayName();
            var name = player.Name;

            if (string.IsNullOrWhiteSpace(name))
                return typeLabel;

            if (name == typeLabel || name.Contains($"（{typeLabel}）"))
                return name;

            return $"{name}（{typeLabel}）";
        }
    }
}
