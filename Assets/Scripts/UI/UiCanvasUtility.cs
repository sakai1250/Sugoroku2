using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>Screen Space Canvas の root が scale 0 になると UI が右下に潰れる問題の補正。</summary>
    public static class UiCanvasUtility
    {
        public static void NormalizeCanvasRoot(Canvas canvas)
        {
            if (canvas == null) return;
            var rt = canvas.transform as RectTransform;
            if (rt == null) return;

            if (rt.localScale.sqrMagnitude < 0.01f)
                rt.localScale = Vector3.one;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            NormalizeCanvasScaler(canvas);
        }

        /// <summary>
        /// 16:9 以外の画面比率（iPhone 19.5:9 や iPad 4:3）で文字やボタンが画面外に見切れるのを防止する。
        /// 横長画面では高さ基準（match=1.0）、縦長/正方形寄り画面では幅基準（match=0.0）に自動補正。
        /// </summary>
        public static void NormalizeCanvasScaler(Canvas canvas)
        {
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                int screenW = Screen.width;
                int screenH = Screen.height;
                if (screenW <= 0 || screenH <= 0) return;

                float aspect = (float)screenW / screenH;
                float refAspect = scaler.referenceResolution.x / Mathf.Max(1f, scaler.referenceResolution.y);
                // 基準比率（例: 16:9 = 1.777）より横長（iPhone等）なら高さ基準にして上下の文字見切れを完全防止
                scaler.matchWidthOrHeight = aspect >= refAspect ? 1f : 0f;
            }
        }
    }
}
