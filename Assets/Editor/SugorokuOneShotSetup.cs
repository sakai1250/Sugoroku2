using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sugoroku.Editor.Board;
using Sugoroku.Editor.Game;
using Sugoroku.Editor.Kenney;

namespace Sugoroku.Editor
{
    /// <summary>プロジェクト初期化〜盤面・UI までを 1 クリックで実行。</summary>
    public static class SugorokuOneShotSetup
    {
        private const string GameWorldScenePath = "Assets/Scenes/GameWorldScene.unity";
        private const string GameUIScenePath    = "Assets/Scenes/GameUIScene.unity";

        [MenuItem("Tools/Sugoroku/一発セットアップ", false, 0)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog(
                    "すごろく 一発セットアップ",
                    "以下をまとめて実行します。\n\n" +
                    "・Sorting Layer / Waypoint プレハブ\n" +
                    "・Kenney → Resources 同期\n" +
                    "・全シーン生成\n" +
                    "・S字盤面 20 マス\n" +
                    "・サイコロ / UI 参照接続\n\n" +
                    "既存シーンは上書きされます。続行しますか？",
                    "実行", "キャンセル"))
                return;

            var originalScene = SceneManager.GetActiveScene().path;

            try
            {
                EditorUtility.DisplayProgressBar("すごろくセットアップ", "プロジェクト設定…", 0.1f);
                BoardSortingLayerSetup.EnsureSortingLayers();
                WaypointPrefabBuilder.SetupProject();
                KenneyResourceSync.SyncAll();

                EditorUtility.DisplayProgressBar("すごろくセットアップ", "シーン生成…", 0.35f);
                CreateScenesMenu.CreateAllScenes();

                EditorUtility.DisplayProgressBar("すごろくセットアップ", "盤面・サイコロ…", 0.6f);
                SetupGameWorld();

                EditorUtility.DisplayProgressBar("すごろくセットアップ", "UI 接続…", 0.85f);
                SetupGameUI();

                AssetDatabase.SaveAssets();
                Debug.Log("✅ 一発セットアップ完了。Play は TitleScene から開始してください。");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(originalScene) && System.IO.File.Exists(originalScene))
                    EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            }
        }

        private static void SetupGameWorld()
        {
            var scene = EditorSceneManager.OpenScene(GameWorldScenePath, OpenSceneMode.Single);
            LayeredBoardGeneratorEditor.RebuildSnakeBoardInScene();
            BoardDiceSetup.EnsureInScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupGameUI()
        {
            var scene = EditorSceneManager.OpenScene(GameUIScenePath, OpenSceneMode.Single);
            WireSugorokuScenesEditor.WireGameUIScene();
            JapaneseFontSetup.ApplyFontToAllTMP(silent: true);
            KenneyResourceSync.ApplyUiStyleToActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
