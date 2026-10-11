using UnityEngine;
using TMPro;
using Sugoroku.Data;
using Sugoroku.UI;

namespace Sugoroku.Board
{
    /// <summary>
    /// 駒の頭上に 1P / 2P などのバッジを表示する。
    /// NPCも青染めではなく、頭上バッジで何Pかを明快に識別する。
    /// </summary>
    public static class PlayerPieceLabel
    {
        public static void Attach(Transform pieceRoot, PlayerData player)
        {
            if (pieceRoot == null || player == null) return;

            // 既存のバッジがあれば削除
            var existing = pieceRoot.Find("PieceBadge");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var sr = pieceRoot.GetComponent<SpriteRenderer>();
            float localHalfHeight = 7.5f;
            if (sr != null && sr.sprite != null)
            {
                localHalfHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * 0.5f;
            }

            // バッジルート（頭上中央）
            var badgeRoot = new GameObject("PieceBadge");
            badgeRoot.transform.SetParent(pieceRoot, false);
            badgeRoot.transform.localPosition = new Vector3(0f, localHalfHeight + (localHalfHeight * 0.18f), 0f);

            // バッジのサイズ（駒の高さに比例させることでPPUに非依存化）
            float badgeScale = localHalfHeight * 2f;
            badgeRoot.transform.localScale = Vector3.one;

            // スロット別の明快な識別カラー
            Color slotColor = GetSlotBadgeColor(player.Index);

            // 1. シャドウ（視認性向上用）
            var shadow = new GameObject("BadgeShadow");
            shadow.transform.SetParent(badgeRoot.transform, false);
            shadow.transform.localPosition = new Vector3(0.04f * badgeScale, -0.04f * badgeScale, 0f);
            shadow.transform.localScale = new Vector3(0.50f * badgeScale, 0.28f * badgeScale, 1f);
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sprite = BoardVisualUtility.GetPixelCardSprite();
            shadowSr.color = new Color(0f, 0f, 0f, 0.45f);
            BoardVisualUtility.ApplySpriteRenderer(shadowSr, BoardSortingLayers.Player, 25);

            // 2. プレート本体
            var plate = new GameObject("BadgePlate");
            plate.transform.SetParent(badgeRoot.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(0.48f * badgeScale, 0.26f * badgeScale, 1f);
            var plateSr = plate.AddComponent<SpriteRenderer>();
            plateSr.sprite = BoardVisualUtility.GetPixelCardSprite();
            plateSr.color = slotColor;
            BoardVisualUtility.ApplySpriteRenderer(plateSr, BoardSortingLayers.Player, 26);

            // 3. プレート内側の枠線/ハイライト
            var inner = new GameObject("BadgeInner");
            inner.transform.SetParent(badgeRoot.transform, false);
            inner.transform.localPosition = Vector3.zero;
            inner.transform.localScale = new Vector3(0.44f * badgeScale, 0.22f * badgeScale, 1f);
            var innerSr = inner.AddComponent<SpriteRenderer>();
            innerSr.sprite = BoardVisualUtility.GetPixelCardSprite();
            innerSr.color = new Color(0.10f, 0.12f, 0.18f, 0.88f);
            BoardVisualUtility.ApplySpriteRenderer(innerSr, BoardSortingLayers.Player, 27);

            // 4. テキスト（"1P", "2P", "3P", "4P"）
            var textGo = new GameObject("PieceLabel");
            textGo.transform.SetParent(badgeRoot.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.01f * badgeScale, 0f);
            textGo.transform.localScale = Vector3.one;

            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = $"{player.Index + 1}P";
            tmp.fontSize = 2.4f * badgeScale;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            var font = TitleMenuController.LoadJapaneseFont();
            if (font != null) tmp.font = font;

            var renderer = textGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = BoardSortingLayers.Player;
                renderer.sortingOrder = 30;
            }
        }

        public static Color GetSlotBadgeColor(int index) => index switch
        {
            0 => new Color(0.95f, 0.32f, 0.32f, 0.98f), // 1P: 赤
            1 => new Color(0.28f, 0.62f, 0.98f, 0.98f), // 2P: 青
            2 => new Color(0.32f, 0.82f, 0.42f, 0.98f), // 3P: 緑
            3 => new Color(0.98f, 0.78f, 0.22f, 0.98f), // 4P: 黄
            _ => new Color(0.60f, 0.60f, 0.60f, 0.98f)
        };
    }
}
