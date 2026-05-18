using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>シーン開始時に Kenney UI / アイコンを一括適用。</summary>
    [DisallowMultipleComponent]
    public class KenneyUiBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                UiCanvasUtility.NormalizeCanvasRoot(canvas);
                KenneyUiStyler.StyleCanvas(canvas);
                KenneyUiStyler.EnsureDiceDisplay(canvas.transform);
            }
        }
    }
}
