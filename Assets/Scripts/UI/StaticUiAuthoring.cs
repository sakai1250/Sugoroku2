using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>
    /// Added by Tools > UI > Bake Static UI. When present, authored RectTransforms in
    /// this Canvas are authoritative. Runtime scripts may update data, colors and
    /// interactivity, but must not generate/reposition the fixed UI layout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StaticUiAuthoring : MonoBehaviour
    {
        [SerializeField] private bool preserveSceneTransforms = true;
        public bool PreserveSceneTransforms => preserveSceneTransforms;

        public static bool IsBaked(Transform child)
        {
            if (child == null) return false;
            var owner = child.GetComponentInParent<StaticUiAuthoring>(true);
            return owner != null && owner.preserveSceneTransforms;
        }

        /// <summary>Preserve authored positions while a dynamic visual effect refreshes colors/text.</summary>
        public static RectSnapshot Capture(Transform root) =>
            IsBaked(root) ? new RectSnapshot(root) : default;

        public readonly struct RectSnapshot
        {
            private readonly RectTransform[] rects;
            private readonly Vector2[] anchorsMin, anchorsMax, pivots, positions, sizes;
            private readonly Vector3[] scales;
            private readonly Quaternion[] rotations;

            public RectSnapshot(Transform root)
            {
                rects = root.GetComponentsInChildren<RectTransform>(true);
                int n = rects.Length;
                anchorsMin = new Vector2[n];
                anchorsMax = new Vector2[n];
                pivots = new Vector2[n];
                positions = new Vector2[n];
                sizes = new Vector2[n];
                scales = new Vector3[n];
                rotations = new Quaternion[n];
                for (int i = 0; i < n; i++)
                {
                    var rt = rects[i];
                    anchorsMin[i] = rt.anchorMin;
                    anchorsMax[i] = rt.anchorMax;
                    pivots[i] = rt.pivot;
                    positions[i] = rt.anchoredPosition;
                    sizes[i] = rt.sizeDelta;
                    scales[i] = rt.localScale;
                    rotations[i] = rt.localRotation;
                }
            }

            public void Restore()
            {
                if (rects == null) return;
                for (int i = 0; i < rects.Length; i++)
                {
                    var rt = rects[i];
                    if (rt == null) continue;
                    rt.anchorMin = anchorsMin[i];
                    rt.anchorMax = anchorsMax[i];
                    rt.pivot = pivots[i];
                    rt.anchoredPosition = positions[i];
                    rt.sizeDelta = sizes[i];
                    rt.localScale = scales[i];
                    rt.localRotation = rotations[i];
                }
            }
        }
    }
}
