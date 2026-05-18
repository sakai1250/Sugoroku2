using UnityEngine;

namespace Sugoroku.UI
{
    public static class JuiceMath
    {
        public static float EaseOutQuad(float t) => 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));

        public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
