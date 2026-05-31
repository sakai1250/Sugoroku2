using UnityEngine;
using TMPro;
using Sugoroku.Data;
using Sugoroku.UI;

namespace Sugoroku.Board
{
    /// <summary>駒の上に 1P / 2P などのラベルを表示。</summary>
    public static class PlayerPieceLabel
    {
        public static void Attach(Transform pieceRoot, PlayerData player)
        {
            if (pieceRoot == null || player == null) return;

            var badge = new GameObject("PieceBadge");
            badge.transform.SetParent(pieceRoot, false);
            badge.transform.localPosition = new Vector3(-0.34f, -0.40f, 0f);
            badge.transform.localScale = new Vector3(0.20f, 0.14f, 1f);

            var badgeSprite = badge.AddComponent<SpriteRenderer>();
            badgeSprite.sprite = BoardVisualUtility.GetCircleSprite();
            badgeSprite.color = new Color(player.PieceTint.r, player.PieceTint.g, player.PieceTint.b, 0.92f);
            BoardVisualUtility.ApplySpriteRenderer(badgeSprite, BoardSortingLayers.Player, 24);

            var go = new GameObject("PieceLabel");
            go.transform.SetParent(badge.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            go.transform.localScale = new Vector3(1f / badge.transform.localScale.x, 1f / badge.transform.localScale.y, 1f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = $"{player.Index + 1}P";
            tmp.fontSize = 0.42f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            var font = TitleMenuController.LoadJapaneseFont();
            if (font != null) tmp.font = font;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = BoardSortingLayers.Player;
                renderer.sortingOrder = 35;
            }
        }
    }
}
