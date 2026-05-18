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

            var go = new GameObject("PieceLabel");
            go.transform.SetParent(pieceRoot, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = player.Name;
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = player.PieceTint;
            tmp.fontStyle = FontStyles.Bold;

            var font = TitleMenuController.LoadJapaneseFont();
            if (font != null) tmp.font = font;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = BoardSortingLayers.Player;
                renderer.sortingOrder = 25;
            }
        }
    }
}
