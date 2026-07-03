using Sugoroku.UI;
using UnityEditor;

namespace Sugoroku.Editor
{
    /// <summary>
    /// Play ボタン押下時に JapaneseFontProvider のキャッシュを必ず破棄・再ウォームアップする。
    /// 「Enter Play Mode Options」でドメインリロードを無効化していても、フォントアセットや
    /// プリウォーム文字セットへの変更を毎回反映させるための仕組み。
    /// </summary>
    [InitializeOnLoad]
    public static class JapaneseFontPlayModeRefresher
    {
        static JapaneseFontPlayModeRefresher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            JapaneseFontProvider.ResetCache();
            JapaneseFontProvider.WarmUpAndSetDefault();
        }
    }
}
