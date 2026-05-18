using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>キャラ選択などから GameWorld へ安全に遷移（コルーチンがシーンアンロードで切れないよう DDOL で実行）。</summary>
    public class SceneTransition : MonoBehaviour
    {
        private static SceneTransition _instance;

        public static void LoadGameWorldAfterConfirm(CharacterSelectJuice juice, RectTransform card,
            CharacterType character)
        {
            EnsureHost();
            _instance.StartCoroutine(_instance.LoadGameWorldRoutine(juice, card, character));
        }

        private static void EnsureHost()
        {
            if (_instance != null) return;
            var go = new GameObject("SceneTransition");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SceneTransition>();
        }

        private IEnumerator LoadGameWorldRoutine(CharacterSelectJuice juice, RectTransform card,
            CharacterType character)
        {
            var juiceDone = false;
            if (juice != null && card != null)
                juice.PlayConfirmSequence(card, character, () => juiceDone = true);
            else
                juiceDone = true;

            float timeout = 4f;
            while (!juiceDone && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameManager.Instance != null)
                Destroy(GameManager.Instance.gameObject);

            var uiScene = SceneManager.GetSceneByName("GameUIScene");
            if (uiScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync("GameUIScene");

            SceneManager.LoadScene("GameWorldScene");
            Destroy(gameObject);
        }
    }
}
