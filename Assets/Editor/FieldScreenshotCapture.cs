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
    /// バッチモード用: GameWorldScene を開き、各シーズン（研究執筆・学会・バイト）の
    /// 美麗な新イラスト背景付き盤面スクリーンショットを同期的にキャプチャして保存する。
    /// </summary>
    public static class FieldScreenshotCapture
    {
        public static void CaptureAllSeasons()
        {
            Debug.Log("[FieldCapture] Opening GameWorldScene...");
            var worldScene = EditorSceneManager.OpenScene("Assets/Scenes/GameWorldScene.unity", OpenSceneMode.Single);

            CampusTilesetManager.WarmUp();
            CampusIllustrationLoader.WarmUp();

            SeasonType[] seasons =
            {
                SeasonType.ResearchWriting,
                SeasonType.Conference,
                SeasonType.PartTime
            };

            string[] names =
            {
                "stage1_research_capture.png",
                "stage2_conference_capture.png",
                "stage3_parttime_capture.png"
            };

            for (int i = 0; i < seasons.Length; i++)
            {
                var season = seasons[i];
                var outputName = names[i];

                GameSession.CurrentSeason = season;
                BoardLayoutGenerator.Invalidate();

                var generator = UnityEngine.Object.FindFirstObjectByType<LayeredBoardGenerator>();
                if (generator != null)
                {
                    generator.GenerateLayeredBoard();
                }

                var route = UnityEngine.Object.FindFirstObjectByType<WaypointRoute>();
                if (route != null)
                {
                    var env = UnityEngine.Object.FindFirstObjectByType<BoardEnvironment>();
                    if (env != null)
                    {
                        env.AlignBackgroundToRoute(route, false);
                    }
                }

                var camCtrl = UnityEngine.Object.FindFirstObjectByType<BoardCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.FrameBoard();
                }

                var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
                if (cam == null)
                {
                    Debug.LogError("[FieldCapture] Main Camera not found.");
                    EditorApplication.Exit(1);
                    return;
                }

                const int width = 1280;
                const int height = 720;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                var bgSr = GameObject.Find("BackgroundArt")?.GetComponent<SpriteRenderer>()
                           ?? GameObject.Find("Diploma")?.GetComponent<SpriteRenderer>()
                           ?? GameObject.Find("PixelPlatformBackdrop")?.GetComponent<SpriteRenderer>();

                if (bgSr != null && bgSr.sprite != null)
                {
                    var bgBounds = bgSr.bounds;
                    cam.transform.position = new Vector3(bgBounds.center.x, bgBounds.center.y, -10f);
                    cam.orthographic = true;
                    // 16:9 (1280x720) 画面の横幅にジャストフィットさせて左右の黒帯を無くす
                    float aspect = (float)width / height;
                    float orthoForWidth = bgBounds.extents.x / aspect;
                    cam.orthographicSize = Mathf.Min(orthoForWidth, bgBounds.extents.y);
                }
                else if (route != null)
                {
                    var b = route.GetRouteBounds(2f);
                    cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
                    cam.orthographic = true;
                    cam.orthographicSize = Mathf.Max(b.size.y * 0.55f, b.size.x * 0.35f, 6f);
                }

                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var prevTarget = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prevTarget;

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string dest = Path.Combine(Directory.GetParent(Application.dataPath).FullName, outputName);
                File.WriteAllBytes(dest, bytes);
                Debug.Log($"[FieldCapture] Screen capture for {season} successfully saved to: {dest} ({bytes.Length} bytes)");

                // デフォルトの field_capture.png にも最初のステージを保存
                if (i == 0)
                {
                    string defaultDest = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "field_capture.png");
                    File.WriteAllBytes(defaultDest, bytes);
                }
            }

            // Stage 1 の初期状態でシーンを保存
            GameSession.CurrentSeason = SeasonType.ResearchWriting;
            BoardLayoutGenerator.Invalidate();
            var gen = UnityEngine.Object.FindFirstObjectByType<LayeredBoardGenerator>();
            gen?.GenerateLayeredBoard();
            var finalRoute = UnityEngine.Object.FindFirstObjectByType<WaypointRoute>();
            var finalEnv = UnityEngine.Object.FindFirstObjectByType<BoardEnvironment>();
            finalEnv?.AlignBackgroundToRoute(finalRoute, false);
            EditorSceneManager.SaveScene(worldScene);

            Debug.Log("[FieldCapture] All 3 season screen captures finished and scene saved successfully!");
            EditorApplication.Exit(0);
        }
    }
}
