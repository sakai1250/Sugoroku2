using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>同一キャラの区別（1P/2P…）と駒の色分け。</summary>
    public static class PlayerIdentity
    {
        public static readonly Color[] SlotColors =
        {
            Color.white,
            new Color(0.45f, 0.85f, 1f),
            new Color(1f, 0.88f, 0.35f),
            new Color(1f, 0.55f, 0.82f),
        };

        public static void Apply(PlayerData[] players)
        {
            if (players == null || players.Length == 0) return;

            var groups = new Dictionary<CharacterType, List<PlayerData>>();
            foreach (var p in players)
            {
                if (!groups.TryGetValue(p.Character, out var list))
                {
                    list = new List<PlayerData>();
                    groups[p.Character] = list;
                }
                list.Add(p);
            }

            foreach (var list in groups.Values)
            {
                list.Sort((a, b) => a.Index.CompareTo(b.Index));
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count; i++)
                        list[i].Name = $"{i + 1}P";
                }
                else
                {
                    var p = list[0];
                    p.Name = p.IsCpu
                        ? $"CPU（{p.Character.DisplayName()}）"
                        : p.Character.DisplayName();
                }
            }

            for (int i = 0; i < players.Length; i++)
                players[i].PieceTint = SlotColors[i % SlotColors.Length];
        }

        public static string FormatHudLabel(PlayerData player)
        {
            if (player == null) return "";

            var typeLabel = player.Character.DisplayName();
            var name = player.Name;

            if (string.IsNullOrWhiteSpace(name))
                return typeLabel;

            // Apply() で単独プレイヤー／CPU は既にクラス名を含む
            if (name == typeLabel || name.Contains($"（{typeLabel}）"))
                return name;

            return $"{name}（{typeLabel}）";
        }
    }
}
