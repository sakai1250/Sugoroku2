using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sugoroku.EditorTools
{
    public static class WebGLBuildHelper
    {
        private static readonly string[] DefaultScenePaths = new[]
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/CharacterSelectScene.unity",
            "Assets/Scenes/GameWorldScene.unity",
            "Assets/Scenes/GameUIScene.unity",
            "Assets/Scenes/ResultScene.unity",
            "Assets/Scenes/GameOverScene.unity"
        };

        [MenuItem("Tools/Build WebGL (unityroom用 zip自動生成)")]
        public static void BuildForUnityroomMenu()
        {
            bool success = ExecuteBuild(isBatchMode: false);
            if (success)
            {
                EditorUtility.DisplayDialog(
                    "WebGL ビルド完了",
                    "unityroom用の WebGL ビルドと zip 圧縮が正常に完了しました！\n\nFinder で表示された 'Build_WebGL.zip' をそのまま unityroom にドラッグ＆ドロップしてください。",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "WebGL ビルド失敗",
                    "WebGL ビルド中にエラーが発生しました。Console ウィンドウを確認してください。",
                    "OK"
                );
            }
        }

        public static void BuildForUnityroomBatch()
        {
            bool success = ExecuteBuild(isBatchMode: true);
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static bool ExecuteBuild(bool isBatchMode)
        {
            Debug.Log("[WebGLBuildHelper] WebGL ビルドの準備を開始します...");

            // 1. 出力先ディレクトリの設定
            // プロジェクトルート (sugoroku2/Build_WebGL)
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string parentDir = Path.GetFullPath(Path.Combine(projectRoot, ".."));
            string buildOutputDir = Path.Combine(parentDir, "Build_WebGL");
            string zipOutputPath = Path.Combine(parentDir, "Build_WebGL.zip");

            Debug.Log($"[WebGLBuildHelper] 出力先ディレクトリ: {buildOutputDir}");
            Debug.Log($"[WebGLBuildHelper] zip 出力先: {zipOutputPath}");

            // 2. unityroom 向けの設定適用 (unityroom 公式指定: Gzip 圧縮, Decompression Fallback: OFF)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.runInBackground = true;

            // 3. シーン一覧の取得
            var sceneList = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();

            if (sceneList.Length == 0)
            {
                sceneList = DefaultScenePaths.Where(File.Exists).ToArray();
            }

            Debug.Log($"[WebGLBuildHelper] ビルド対象シーン ({sceneList.Length} 件): \n" + string.Join("\n", sceneList));

            // 出力ディレクトリの作成/クリーンアップ
            if (Directory.Exists(buildOutputDir))
            {
                try
                {
                    Directory.Delete(buildOutputDir, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WebGLBuildHelper] 既存ディレクトリのクリーンアップ警告: {ex.Message}");
                }
            }
            Directory.CreateDirectory(buildOutputDir);

            // 4. ビルド実行
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sceneList,
                locationPathName = buildOutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[WebGLBuildHelper] ビルド失敗: 結果={summary.result}, エラー数={summary.totalErrors}");
                return false;
            }

            Debug.Log($"[WebGLBuildHelper] WebGL ビルド成功! (所要時間: {summary.totalTime.TotalSeconds:F1}秒, サイズ: {summary.totalSize / (1024 * 1024):F1} MB)");

            // 5. zip 圧縮
            try
            {
                if (File.Exists(zipOutputPath))
                {
                    File.Delete(zipOutputPath);
                }

                // includeBaseDirectory: false にすることで zip のルート直下に index.html や Build フォルダが配置される
                ZipFile.CreateFromDirectory(buildOutputDir, zipOutputPath, System.IO.Compression.CompressionLevel.Optimal, false);
                Debug.Log($"[WebGLBuildHelper] unityroom 用 zip 作成成功: {zipOutputPath}");

                if (!isBatchMode)
                {
                    EditorUtility.RevealInFinder(zipOutputPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLBuildHelper] zip 圧縮中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }
    }
}

