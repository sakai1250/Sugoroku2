using System;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>カットインの入場スタイル（PowerPoint 風のバリエーション）。</summary>
    public enum CutInStyle
    {
        CenterPop,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        ZoomIn,
        FlipIn,
        BounceDown,
        Fade,
        FloatIn,
        GrowFromBig,
        Spin,
        Swivel,
        Split
    }

    /// <summary>入場の開始状態（rest への相対）。scale は軸別 (x,y)。</summary>
    public readonly struct CutInEntrance
    {
        public readonly Vector2 Offset;
        public readonly Vector2 Scale;
        public readonly float RotZ;
        public readonly Func<float, float> Ease;

        public CutInEntrance(Vector2 offset, Vector2 scale, float rotZ, Func<float, float> ease)
        {
            Offset = offset;
            Scale = scale;
            RotZ = rotZ;
            Ease = ease;
        }
    }

    /// <summary>退場の終了状態（rest からの相対）。scale は軸別 (x,y)。</summary>
    public readonly struct CutInExit
    {
        public readonly Vector2 Offset;
        public readonly Vector2 Scale;
        public readonly float RotZ;
        public readonly Func<float, float> Ease;

        public CutInExit(Vector2 offset, Vector2 scale, float rotZ, Func<float, float> ease)
        {
            Offset = offset;
            Scale = scale;
            RotZ = rotZ;
            Ease = ease;
        }
    }
}
