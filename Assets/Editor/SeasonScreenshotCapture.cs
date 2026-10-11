using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sugoroku.Board;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.EditorTools
{
    /// <summary>
    /// 各シーズン（Stage 1: 研究執筆, Stage 2: 学会, Stage 3: バイト）の
    /// 盤面スクリーンショットを撮影・保存するエディタツール。
    /// プレイモードで確実に美麗なゲームビューをキャプチャする。
    /// </summary>
    public static class SeasonScreenshotCapture
    {
        private const string OutputPrefix = "Temp/stage";
        private static bool _enteredPlay;
        private static double _phaseStartTime;
        private static int _currentStageIndex; // 0: Research, 1: Conference, 2: PartTime
        private static int _attempt;

        private static readonly SeasonType[] Seasons =
        {
            SeasonType.ResearchWriting,
            SeasonType.Conference,
            SeasonType.PartTime
        };

        private static readonly string[] Names =
        {
            "stage1_research",
            "stage2_conference",
            "stage3_parttime"
        };

        public static void CaptureAllSeasons()
        {
            Debug.Log("[SeasonCapture] Opening GameWorldScene...");
            EditorSceneManager.OpenScene("Assets/Scenes/GameWorldScene.unity", OpenSceneMode.Single);
            _enteredPlay = false;
            _currentStageIndex = 0;
            _attempt = 0;
            _phaseStartTime = 0.0;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!_enteredPlay)
                {
                    _enteredPlay = true;
                    Debug.Log("[SeasonCapture] Entering play mode...");
                    EditorApplication.isPlaying = true;
                }
                else if (EditorApplication.timeSinceStartup > 120.0)
                {
                    Debug.LogError("[SeasonCapture] Timed out entering play mode.");
                    EditorApplication.Exit(2);
                }
                return;
            }

            if (_phaseStartTime <= 0.0)
            {
                _phaseStartTime = EditorApplication.timeSinceStartup;
                ApplyStage(Seasons[_currentStageIndex]);
                return;
            }

            // 盤面生成・カメラ演出が落ち着くまで待つ
            if (EditorApplication.timeSinceStartup - _phaseStartTime < 4.0) return;

            string targetTemp = $"{OutputPrefix}_{Names[_currentStageIndex]}.png";

            if (_attempt < 2)
            {
                _attempt++;
                ScreenCapture.CaptureScreenshot(targetTemp);
                return;
            }

            string src = Path.Combine(Directory.GetParent(Application.dataPath).FullName, targetTemp);
            if (File.Exists(src))
            {
                string dest = Path.Combine(Directory.GetParent(Application.dataPath).FullName, $"{Names[_currentStageIndex]}_capture.png");
                File.Copy(src, dest, true);
                var fi = new FileInfo(dest);
                Debug.Log($"[SeasonCapture] Successfully saved {Names[_currentStageIndex]}: {dest} ({fi.Length} bytes)");

                _currentStageIndex++;
                if (_currentStageIndex >= Seasons.Length)
                {
                    Debug.Log("[SeasonCapture] All 3 stages captured successfully!");
                    EditorApplication.isPlaying = false;
                    EditorApplication.Exit(0);
                    return;
                }

                _attempt = 0;
                _phaseStartTime = EditorApplication.timeSinceStartup;
                ApplyStage(Seasons[_currentStageIndex]);
            }
            else
            {
                Debug.LogWarning($"[SeasonCapture] Waiting for capture file: {src}");
            }
        }

        private static void ApplyStage(SeasonType season)
        {
            GameSession.CurrentSeason = season;
            CampusTilesetManager.WarmUp();
            CampusIllustrationLoader.WarmUp();

            var generator = UnityEngine.Object.FindFirstObjectByType<LayeredBoardGenerator>();
            var route = UnityEngine.Object.FindFirstObjectByType<WaypointRoute>();
            var env = UnityEngine.Object.FindFirstObjectByType<BoardEnvironment>();

            BoardLayoutGenerator.Invalidate();

            if (generator != null)
            {
                generator.GenerateLayeredBoard();
            }

            if (route != null && env != null)
            {
                env.AlignBackgroundToRoute(route, false);
            }

            Debug.Log($"[SeasonCapture] Applied stage {season}");
        }
    }
}
