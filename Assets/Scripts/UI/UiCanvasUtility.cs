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
        }
    }
}
