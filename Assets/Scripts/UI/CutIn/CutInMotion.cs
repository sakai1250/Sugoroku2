using System;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>入場/退場スタイルのパラメータ定義と Lerp 評価（純ロジック・副作用なし）。</summary>
    public static class CutInMotion
    {
        private static float Linear(float t) => t;

        /// <summary>入場開始状態（rest への相対オフセット・開始スケール・開始回転Z・イージング）。</summary>
        public static CutInEntrance Entrance(CutInStyle s) => s switch
        {
            CutInStyle.CenterPop   => new CutInEntrance(Vector2.zero,          new Vector2(0.82f, 0.82f), 0f,    JuiceMath.EaseOutQuad),
            CutInStyle.SlideLeft   => new CutInEntrance(new Vector2(-1000f, 0f), Vector2.one,             0f,    JuiceMath.EaseOutCubic),
            CutInStyle.SlideRight  => new CutInEntrance(new Vector2(1000f, 0f),  Vector2.one,             0f,    JuiceMath.EaseOutCubic),
            CutInStyle.SlideUp     => new CutInEntrance(new Vector2(0f, -620f),  Vector2.one,             0f,    JuiceMath.EaseOutBack),
            CutInStyle.SlideDown   => new CutInEntrance(new Vector2(0f, 620f),   Vector2.one,             0f,    JuiceMath.EaseOutCubic),
            CutInStyle.ZoomIn      => new CutInEntrance(Vector2.zero,          new Vector2(0.35f, 0.35f), 0f,    JuiceMath.EaseOutQuad),
            CutInStyle.FlipIn      => new CutInEntrance(Vector2.zero,          Vector2.one,             -100f,   JuiceMath.EaseOutBack),
            CutInStyle.BounceDown  => new CutInEntrance(new Vector2(0f, 680f),   Vector2.one,             0f,    JuiceMath.EaseOutBounce),
            CutInStyle.Fade        => new CutInEntrance(Vector2.zero,          Vector2.one,               0f,    JuiceMath.EaseOutQuad),
            CutInStyle.FloatIn     => new CutInEntrance(new Vector2(0f, -90f),   Vector2.one,             0f,    JuiceMath.EaseOutCubic),
            CutInStyle.GrowFromBig => new CutInEntrance(Vector2.zero,          new Vector2(1.30f, 1.30f), 0f,    JuiceMath.EaseOutQuad),
            CutInStyle.Spin        => new CutInEntrance(Vector2.zero,          new Vector2(0.40f, 0.40f), -360f, JuiceMath.EaseOutCubic),
            CutInStyle.Swivel      => new CutInEntrance(Vector2.zero,          new Vector2(0f, 1f),       0f,    JuiceMath.EaseOutBack),
            CutInStyle.Split       => new CutInEntrance(Vector2.zero,          new Vector2(1f, 0f),       0f,    JuiceMath.EaseOutBack),
            _                      => new CutInEntrance(Vector2.zero,          new Vector2(0.82f, 0.82f), 0f,    JuiceMath.EaseOutQuad),
        };

        /// <summary>退場終了状態（rest からの相対）。入場と対になる方向へ抜ける。</summary>
        public static CutInExit Exit(CutInStyle s) => s switch
        {
            CutInStyle.SlideLeft   => new CutInExit(new Vector2(1000f, 0f),  Vector2.one,             0f,   Linear),
            CutInStyle.SlideRight  => new CutInExit(new Vector2(-1000f, 0f), Vector2.one,             0f,   Linear),
            CutInStyle.SlideUp     => new CutInExit(new Vector2(0f, 620f),   Vector2.one,             0f,   Linear),
            CutInStyle.SlideDown   => new CutInExit(new Vector2(0f, -620f),  Vector2.one,             0f,   Linear),
            CutInStyle.ZoomIn      => new CutInExit(Vector2.zero,          new Vector2(1.25f, 1.25f), 0f,   Linear),
            CutInStyle.GrowFromBig => new CutInExit(Vector2.zero,          new Vector2(1.25f, 1.25f), 0f,   Linear),
            CutInStyle.FlipIn      => new CutInExit(Vector2.zero,          Vector2.one,             100f,   Linear),
            CutInStyle.Spin        => new CutInExit(Vector2.zero,          new Vector2(0.6f, 0.6f),  360f,  Linear),
            CutInStyle.Swivel      => new CutInExit(Vector2.zero,          new Vector2(0f, 1f),       0f,   Linear),
            CutInStyle.Split       => new CutInExit(Vector2.zero,          new Vector2(1f, 0f),       0f,   Linear),
            _                      => new CutInExit(Vector2.zero,          Vector2.one,               0f,   Linear),
        };

        /// <summary>
        /// 入場進捗 tIn(0..1) と 退場進捗 tOut(0..1) から現在の変形を返す。
        /// tIn=1,tOut=0 で rest（offset=0, scale=(1,1), rotZ=0）へ収束する。
        /// </summary>
        public static (Vector2 offset, Vector2 scale, float rotZ) Evaluate(CutInStyle style, float tIn, float tOut)
        {
            var en = Entrance(style);
            var ex = Exit(style);

            float eIn = en.Ease(Mathf.Clamp01(tIn));
            Vector2 offset = Vector2.Lerp(en.Offset, Vector2.zero, eIn);
            Vector2 scale = Vector2.Lerp(en.Scale, Vector2.one, eIn);
            float rot = Mathf.Lerp(en.RotZ, 0f, eIn);

            if (tOut > 0f)
            {
                float eOut = ex.Ease(Mathf.Clamp01(tOut));
                offset = Vector2.Lerp(offset, ex.Offset, eOut);
                scale = Vector2.Lerp(scale, ex.Scale, eOut);
                rot = Mathf.Lerp(rot, ex.RotZ, eOut);
            }

            return (offset, scale, rot);
        }
    }
}
