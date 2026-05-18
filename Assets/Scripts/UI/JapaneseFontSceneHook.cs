using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Sugoroku.UI
{
    /// <summary>シーン読み込み時に日本語フォントを最優先で適用する。</summary>
    public static class JapaneseFontSceneHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            JapaneseFontProvider.WarmUpAndSetDefault();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            JapaneseFontProvider.WarmUpAndSetDefault();
            JapaneseFontProvider.ApplyAllLoaded();
            EnsureEnforcersOnAllText();
        }

        private static void EnsureEnforcersOnAllText()
        {
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.GetComponent<JapaneseFontEnforcer>() == null)
                    tmp.gameObject.AddComponent<JapaneseFontEnforcer>();
            }
        }
    }
}
