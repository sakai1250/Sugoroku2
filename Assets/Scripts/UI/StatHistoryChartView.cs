using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>
    /// IF/メンタルの推移を折れ線で描画する自前UGUIチャート。依存ライブラリなし、3D非依存。
    /// ResultSceneController(画面表示)とShareImageBuilder(シェア画像)の両方から呼ばれる。
    /// </summary>
    public static class StatHistoryChartView
    {
        public static readonly Color IfLineColor     = new(0.40f, 1f, 0.58f, 1f);
        public static readonly Color MentalLineColor = new(1f, 0.38f, 0.38f, 1f);

        /// <summary>
        /// plotArea の子として折れ線セグメントを生成する。
        /// history が2件未満ならプレースホルダー文言を描画する（showPlaceholder=false なら何も描画しない）。
        /// IF=実線・メンタル=破線＋データ点マーカーで、色に依存せず系列を区別できる。
        /// </summary>
        public static void Draw(RectTransform plotArea, IReadOnlyList<StatSnapshot> history, int maxMental,
            float lineThickness = 4f, bool showPlaceholder = true)
        {
            Clear(plotArea);
            if (plotArea == null) return;

            if (history == null || history.Count < 2)
            {
                if (showPlaceholder) DrawPlaceholder(plotArea);
                return;
            }

            int maxIf = 1;
            for (int i = 0; i < history.Count; i++)
                if (history[i].IfScore > maxIf) maxIf = history[i].IfScore;

            float width  = plotArea.rect.width;
            float height = plotArea.rect.height;
            float step   = width / (history.Count - 1);

            var ifPoints     = new Vector2[history.Count];
            var mentalPoints = new Vector2[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                float x = step * i;
                ifPoints[i]     = new Vector2(x, Mathf.Clamp01((float)history[i].IfScore / maxIf) * height);
                mentalPoints[i] = new Vector2(x, Mathf.Clamp01((float)history[i].Mental / Mathf.Max(1, maxMental)) * height);
            }

            DrawPolyline(plotArea, ifPoints, IfLineColor, lineThickness, dashed: false);
            DrawPolyline(plotArea, mentalPoints, MentalLineColor, lineThickness, dashed: true);
            DrawMarkers(plotArea, ifPoints, IfLineColor, markerSize: 12f);
            DrawMarkers(plotArea, mentalPoints, MentalLineColor, markerSize: 9f);

            DrawLegend(plotArea);
        }

        private static void DrawPlaceholder(RectTransform plotArea)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform));
            go.transform.SetParent(plotArea, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "IF・メンタルの推移 — データはターン経過後に表示されます";
            tmp.fontSize = Sugoroku.UI.HudTextStyle.Scale(13f);
            tmp.color = new Color(0.62f, 0.68f, 0.76f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
        }

        private static void DrawLegend(RectTransform plotArea)
        {
            CreateLegendLabel(plotArea, "─ IF", IfLineColor, new Vector2(0f, 0f), new Vector2(8f, 6f));
            CreateLegendLabel(plotArea, "- - メンタル", MentalLineColor, new Vector2(1f, 0f), new Vector2(-8f, 6f));
        }

        private static void CreateLegendLabel(RectTransform parent, string text, Color color, Vector2 anchor, Vector2 position)
        {
            var go = new GameObject("LegendLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = Sugoroku.UI.HudTextStyle.Scale(12f);
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(120f, 20f);
        }

        /// <summary>plotArea配下の描画済みセグメントをすべて削除する。</summary>
        public static void Clear(RectTransform plotArea)
        {
            if (plotArea == null) return;
            for (int i = plotArea.childCount - 1; i >= 0; i--)
                Object.Destroy(plotArea.GetChild(i).gameObject);
        }

        private static void DrawPolyline(RectTransform parent, Vector2[] points, Color color, float thickness, bool dashed)
        {
            for (int i = 0; i < points.Length - 1; i++)
                CreateSegment(parent, points[i], points[i + 1], color, thickness, dashed);
        }

        private static void CreateSegment(RectTransform parent, Vector2 a, Vector2 b, Color color, float thickness, bool dashed)
        {
            Vector2 diff = b - a;
            float length = diff.magnitude;
            if (length < 0.001f) return;

            if (!dashed)
            {
                CreateLineSegment(parent, a, diff, length, color, thickness);
                return;
            }

            const float dashLen = 8f;
            const float gapLen = 6f;
            Vector2 dir = diff / length;
            float t = 0f;
            while (t < length)
            {
                float end = Mathf.Min(t + dashLen, length);
                if (end - t > 0.1f)
                {
                    Vector2 start = a + dir * t;
                    Vector2 segDiff = dir * (end - t);
                    CreateLineSegment(parent, start, segDiff, end - t, color, thickness);
                }
                t += dashLen + gapLen;
            }
        }

        private static void CreateLineSegment(RectTransform parent, Vector2 a, Vector2 diff, float length, Color color, float thickness)
        {
            var go = new GameObject("StatLineSegment", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(length, thickness);
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void DrawMarkers(RectTransform parent, Vector2[] points, Color color, float markerSize)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var go = new GameObject("Marker", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = points[i];
                rt.sizeDelta = new Vector2(markerSize, markerSize);
                var img = go.GetComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;

                var inner = new GameObject("MarkerCore", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(rt, false);
                var innerImg = inner.GetComponent<Image>();
                innerImg.color = color;
                innerImg.raycastTarget = false;
                var innerRt = innerImg.rectTransform;
                innerRt.anchorMin = innerRt.anchorMax = new Vector2(0.5f, 0.5f);
                innerRt.pivot = new Vector2(0.5f, 0.5f);
                innerRt.sizeDelta = new Vector2(markerSize * 0.5f, markerSize * 0.5f);
            }
        }
    }
}