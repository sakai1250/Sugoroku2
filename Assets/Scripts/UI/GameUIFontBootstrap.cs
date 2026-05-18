using TMPro;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>GameUIScene 読み込み時にフォント修復とステータスバナーを有効化。</summary>
    [DefaultExecutionOrder(-200)]
    public class GameUIFontBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var layout = canvas.GetComponent<GameMainLayout>();
            if (layout != null) layout.ApplyLayout();
            else ResourceHudVisuals.SetupTopResourceBar(canvas.transform);

            JapaneseFontProvider.ApplyAllInCanvas(canvas);

            if (GetComponent<GameStatusBanner>() == null)
                gameObject.AddComponent<GameStatusBanner>();

            foreach (var modal in FindObjectsByType<EventModalUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                modal.EnsureInitialized();
        }
    }
}
