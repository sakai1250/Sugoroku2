using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        /// <summary>plotArea の子として折れ線セグメントを生成する。history が2件未満なら何も描画しない。</summary>
        public static void Draw(RectTransform plotArea, IReadOnlyList<StatSnapshot> history, int maxMental, float lineThickness = 4f)
        {
            Clear(plotArea);
            if (plotArea == null || history == null || history.Count < 2) return;

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

            DrawPolyline(plotArea, ifPoints, IfLineColor, lineThickness);
            DrawPolyline(plotArea, mentalPoints, MentalLineColor, lineThickness);
        }

        /// <summary>plotArea配下の描画済みセグメントをすべて削除する。</summary>
        public static void Clear(RectTransform plotArea)
        {
            if (plotArea == null) return;
            for (int i = plotArea.childCount - 1; i >= 0; i--)
                Object.Destroy(plotArea.GetChild(i).gameObject);
        }

        private static void DrawPolyline(RectTransform parent, Vector2[] points, Color color, float thickness)
        {
            for (int i = 0; i < points.Length - 1; i++)
                CreateSegment(parent, points[i], points[i + 1], color, thickness);
        }

        private static void CreateSegment(RectTransform parent, Vector2 a, Vector2 b, Color color, float thickness)
        {
            var go = new GameObject("StatLineSegment", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);

            Vector2 diff = b - a;
            float length = diff.magnitude;
            rt.sizeDelta = new Vector2(length, thickness);
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
