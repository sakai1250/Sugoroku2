using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.UI;

namespace Sugoroku.Board
{
    /// <summary>Mass_TextCard プレハブのランタイム／エディタ生成。</summary>
    public static class MassTextCardPrefabFactory
    {
        public const float CardAspectWidth  = 4f;
        public const float CardAspectHeight = 3f;
        public const float CardWorldWidth   = 2.4f;
        public static float CardWorldHeight => CardWorldWidth * CardAspectHeight / CardAspectWidth;

        public const float CardUiWidth  = 240f;
        public const float CardUiHeight = 180f;

        // カード間に接続線が見える隙間を空ける（敷き詰めず、経路が分かるようにする）。
        public static float RecommendedSpacingX => CardWorldWidth + 0.85f;
        public static float RecommendedSpacingY => CardWorldHeight + 0.85f;

        public static Waypoint CreateRuntimeInstance(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var wp = root.AddComponent<Waypoint>();
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(CardWorldWidth, CardWorldHeight);

            BuildCardUi(root.transform);
            root.AddComponent<MassTextCardView>();
            return wp;
        }

        public static void BuildCardUi(Transform root)
        {
            var canvasGo = new GameObject("CardCanvas");
            canvasGo.transform.SetParent(root, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = BoardSortingLayers.Board;
            canvas.sortingOrder = BoardSortingLayers.WaypointBaseOrder + 80;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(CardUiWidth, CardUiHeight);
            float scale = CardWorldWidth / canvasRt.sizeDelta.x;
            canvasGo.transform.localScale = Vector3.one * scale;

            var borderGo = CreateUiImage(canvasRt, "CardBorder", new Vector2(0, 4));
            borderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(CardUiWidth - 4f, CardUiHeight - 4f);
            borderGo.color = new Color(0.58f, 0.62f, 0.72f, 1f);

            var panelGo = CreateUiImage(canvasRt, "CardPanel", Vector2.zero);
            panelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(CardUiWidth - 12f, CardUiHeight - 12f);
            panelGo.color = new Color(0.42f, 0.46f, 0.56f, 0.94f);

            CreateMassArtImage(panelGo.transform);
            MassTextCardArtLayout.EnsurePanelMask(panelGo.transform);

            var accentStrip = CreateUiImage(panelGo.transform, "AccentStrip", Vector2.zero);
            SetRect(accentStrip, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(5.5f, 0f));
            accentStrip.color = new Color(0.58f, 0.62f, 0.72f, 1f);

            var headerBand = CreateUiImage(panelGo.transform, "HeaderBand", Vector2.zero);
            SetRect(headerBand, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 25f), new Vector2(0f, -12.5f));
            headerBand.color = new Color(0.30f, 0.33f, 0.40f, 0.44f);

            var cornerMarker = CreateUiImage(panelGo.transform, "CornerMarker", Vector2.zero);
            SetRect(cornerMarker, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));
            cornerMarker.color = new Color(0.70f, 0.74f, 0.82f, 0.95f);

            var markerLabel = CreateTmp(panelGo.transform, "MarkerLabel", 10, TextAlignmentOptions.Center);
            markerLabel.fontStyle = FontStyles.Bold;
            markerLabel.color = new Color(0.16f, 0.18f, 0.22f, 1f);
            markerLabel.raycastTarget = false;
            SetRect(markerLabel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));

            var titleRule = CreateUiImage(panelGo.transform, "TitleRule", Vector2.zero);
            SetRect(titleRule, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-24f, 3f), new Vector2(8f, 8f));
            titleRule.color = new Color(0.72f, 0.76f, 0.84f, 0.92f);

            var pixelGroundLip = CreateUiImage(panelGo.transform, "PixelGroundLip", Vector2.zero);
            SetRect(pixelGroundLip, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-26f, 6f), new Vector2(8f, 4f));
            pixelGroundLip.color = new Color(0.24f, 0.78f, 0.22f, 0.96f);

            var dirtChipLeft = CreateUiImage(panelGo.transform, "DirtChipLeft", Vector2.zero);
            SetRect(dirtChipLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(18f, 4f), new Vector2(40f, 14f));
            dirtChipLeft.color = new Color(0.30f, 0.16f, 0.08f, 0.62f);

            var dirtChipRight = CreateUiImage(panelGo.transform, "DirtChipRight", Vector2.zero);
            SetRect(dirtChipRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(22f, 4f), new Vector2(-54f, 14f));
            dirtChipRight.color = new Color(0.24f, 0.12f, 0.06f, 0.58f);

            var terrainBlockLeft = CreateUiImage(panelGo.transform, "TerrainBlockLeft", Vector2.zero);
            SetRect(terrainBlockLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0.5f, 0f), new Vector2(18f, 10f), new Vector2(30f, 4f));
            terrainBlockLeft.color = new Color(0.25f, 0.76f, 0.20f, 0.70f);

            var terrainBlockMid = CreateUiImage(panelGo.transform, "TerrainBlockMid", Vector2.zero);
            SetRect(terrainBlockMid, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(24f, 8f), new Vector2(-8f, 4f));
            terrainBlockMid.color = new Color(0.36f, 0.86f, 0.24f, 0.74f);

            var terrainBlockRight = CreateUiImage(panelGo.transform, "TerrainBlockRight", Vector2.zero);
            SetRect(terrainBlockRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(18f, 10f), new Vector2(-38f, 4f));
            terrainBlockRight.color = new Color(0.22f, 0.68f, 0.18f, 0.70f);

            CreateCoinPip(panelGo.transform, "CoinPipTop", new Vector2(5.5f, 24f), 0.66f);
            CreateCoinPip(panelGo.transform, "CoinPipMid", new Vector2(5.5f, 0f), 0.72f);
            CreateCoinPip(panelGo.transform, "CoinPipLow", new Vector2(5.5f, -24f), 0.62f);

            var tagGo = CreateTmp(panelGo.transform, "TagLabel", 13, TextAlignmentOptions.TopLeft);
            var tagRt = tagGo.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0, 1);
            tagRt.anchorMax = new Vector2(1, 1);
            tagRt.pivot     = new Vector2(0.5f, 1f);
            tagRt.anchoredPosition = new Vector2(18, -5);
            tagRt.sizeDelta = new Vector2(-54, 22);

            var titleGo = CreateTmp(panelGo.transform, "TitleLabel", 17, TextAlignmentOptions.TopLeft);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(18, 9);
            titleRt.offsetMax = new Vector2(-12, -29);
            titleGo.textWrappingMode = TextWrappingModes.Normal;
            titleGo.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static Image CreateMassArtImage(Transform panel) =>
            MassTextCardArtLayout.EnsureMassArtImage(panel);

        public static Image CreateMassArtImageForPanel(Transform panel) =>
            MassTextCardArtLayout.EnsureMassArtImage(panel);

        private static Image CreateUiImage(Transform parent, string name, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = name is "CardBorder" or "CardPanel" or "CornerMarker"
                ? BoardVisualUtility.GetPixelCardSprite()
                : BoardVisualUtility.GetPixelSolidSprite();
            img.type   = Image.Type.Sliced;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = sizeDelta == Vector2.zero ? Vector2.zero : sizeDelta;
            if (sizeDelta == Vector2.zero)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            }
            return img;
        }

        private static Image CreateCoinPip(Transform parent, string name, Vector2 anchoredPosition, float alpha)
        {
            var img = CreateUiImage(parent, name, Vector2.zero);
            img.sprite = BoardVisualUtility.GetPixelCoinSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = new Color(1f, 0.74f, 0.20f, alpha);
            SetRect(img, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(9f, 9f), anchoredPosition);
            return img;
        }

        private static void SetRect(Image img, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetRect(TextMeshProUGUI tmp, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.text = "";
            TitleMenuController.ApplyJapaneseFont(tmp);
            return tmp;
        }
    }
}
