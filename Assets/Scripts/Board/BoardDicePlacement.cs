using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.Board
{
    /// <summary>ワールド上のサイコロを、現在プレイヤーの駒の横に配置する。</summary>
    public static class BoardDicePlacement
    {
        public static Vector3 OffsetFromPiece = new(2.4f, -1.8f, 0f);

        public static void PlaceNearCurrentPlayer()
        {
            var player = GameManager.Instance?.GetCurrentPlayer();
            if (player != null) PlaceNearPlayer(player);
        }

        public static void PlaceNearPlayer(PlayerData player)
        {
            if (player == null || BoardDice.Instance == null) return;
            var piece = GameManager.Instance?.GetPiece(player.Index);
            if (piece == null) return;

            BoardDice.Instance.transform.position = piece.transform.position + OffsetFromPiece;
        }

        public static Vector3 GetDiceWorldPosition(PlayerData player)
        {
            if (player == null) return BoardDice.Instance != null
                ? BoardDice.Instance.transform.position
                : Vector3.zero;

            var piece = GameManager.Instance?.GetPiece(player.Index);
            if (piece == null)
                return BoardDice.Instance != null
                    ? BoardDice.Instance.transform.position
                    : Vector3.zero;

            return piece.transform.position + OffsetFromPiece;
        }
    }
}
