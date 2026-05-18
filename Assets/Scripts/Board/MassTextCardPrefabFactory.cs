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
        public const float CardWorldWidth  = 2.6f;
        public const float CardWorldHeight = 0.95f;

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
            canvasRt.sizeDelta = new Vector2(260f, 95f);
            float scale = CardWorldWidth / canvasRt.sizeDelta.x;
            canvasGo.transform.localScale = Vector3.one * scale;

            var borderGo = CreateUiImage(canvasRt, "CardBorder", new Vector2(0, 4));
            borderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(256, 91);
            borderGo.color = new Color(0.35f, 0.38f, 0.48f, 1f);

            var panelGo = CreateUiImage(canvasRt, "CardPanel", Vector2.zero);
            panelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(248, 83);
            panelGo.color = new Color(0.22f, 0.24f, 0.30f, 0.94f);

            var tagGo = CreateTmp(panelGo.transform, "TagLabel", 13, TextAlignmentOptions.TopLeft);
            var tagRt = tagGo.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0, 1);
            tagRt.anchorMax = new Vector2(1, 1);
            tagRt.pivot     = new Vector2(0.5f, 1f);
            tagRt.anchoredPosition = new Vector2(0, -6);
            tagRt.sizeDelta = new Vector2(-16, 22);

            var titleGo = CreateTmp(panelGo.transform, "TitleLabel", 17, TextAlignmentOptions.TopLeft);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(8, 6);
            titleRt.offsetMax = new Vector2(-8, -26);
            titleGo.textWrappingMode = TextWrappingModes.Normal;
            titleGo.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static Image CreateUiImage(Transform parent, string name, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = BoardVisualUtility.GetSquareSprite();
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
