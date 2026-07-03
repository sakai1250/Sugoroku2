using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>
    /// リザルトの「診断メーカー」風シェア画像(PNG)を、オフスクリーンの Canvas + Camera でランタイム生成する。
    /// </summary>
    public static class ShareImageBuilder
    {
        private const int CardSize = 1080;
        private static readonly Vector3 CaptureOrigin = new(9999f, 9999f, 0f);

        public static byte[] BuildPng(PlayerSnapshot player, GraduationOutcome outcome)
        {
            var captureRoot = new GameObject("ShareCardCaptureRoot");
            captureRoot.transform.position = CaptureOrigin;

            Camera cam = null;
            RenderTexture renderTex = null;
            Texture2D tex = null;
            try
            {
                var canvasGo = new GameObject("ShareCardCanvas", typeof(Canvas));
                canvasGo.transform.SetParent(captureRoot.transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                var canvasRt = canvasGo.GetComponent<RectTransform>();
                canvasRt.sizeDelta = new Vector2(CardSize, CardSize);
                canvasRt.position = CaptureOrigin;
                canvasRt.localRotation = Quaternion.identity;
                canvasRt.localScale = Vector3.one;

                BuildCardContent(canvasGo.transform, player, outcome);

                var camGo = new GameObject("ShareCardCamera", typeof(Camera));
                camGo.transform.SetParent(captureRoot.transform, false);
                cam = camGo.GetComponent<Camera>();
                cam.transform.position = CaptureOrigin + new Vector3(0f, 0f, -100f);
                cam.transform.rotation = Quaternion.identity;
                cam.orthographic = true;
                cam.orthographicSize = CardSize / 2f;
                cam.aspect = 1f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.09f, 0.13f, 1f);
                cam.cullingMask = ~0;
                cam.enabled = false;

                renderTex = new RenderTexture(CardSize, CardSize, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = renderTex;

                Canvas.ForceUpdateCanvases();
                cam.Render();

                var prevActive = RenderTexture.active;
                RenderTexture.active = renderTex;
                tex = new Texture2D(CardSize, CardSize, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, CardSize, CardSize), 0, 0);
                tex.Apply();
                RenderTexture.active = prevActive;

                return tex.EncodeToPNG();
            }
            finally
            {
                if (cam != null) cam.targetTexture = null;
                if (renderTex != null)
                {
                    renderTex.Release();
                    Object.Destroy(renderTex);
                }
                if (tex != null) Object.Destroy(tex);
                Object.Destroy(captureRoot);
            }
        }

        private static void BuildCardContent(Transform root, PlayerSnapshot player, GraduationOutcome outcome)
        {
            CreateImage(root, "Background", Vector2.zero, new Vector2(CardSize, CardSize),
                new Color(0.07f, 0.09f, 0.13f, 1f));

            CreateImage(root, "AccentBarTop", new Vector2(0f, CardSize / 2f - 14f), new Vector2(CardSize, 28f),
                outcome.AccentColor);
            CreateImage(root, "AccentBarBottom", new Vector2(0f, -CardSize / 2f + 14f), new Vector2(CardSize, 28f),
                outcome.AccentColor);

            var header = CreateLabel(root, "Header", "研究者人生シミュレーター『すごろく』", 30f,
                new Vector2(0f, CardSize / 2f - 90f), new Vector2(CardSize - 120f, 60f),
                new Color(0.85f, 0.87f, 0.92f));
            header.fontStyle = FontStyles.Bold;

            var name = CreateLabel(root, "PlayerName", $"{player.Name}（{player.Character.DisplayName()}）", 40f,
                new Vector2(0f, CardSize / 2f - 170f), new Vector2(CardSize - 120f, 70f), Color.white);
            name.fontStyle = FontStyles.Bold;

            var rank = CreateLabel(root, "RankLabel", $"あなたの研究者人生ランク：{outcome.Rank}", 60f,
                new Vector2(0f, 70f), new Vector2(CardSize - 100f, 100f), outcome.AccentColor);
            rank.fontStyle = FontStyles.Bold;

            var career = CreateLabel(root, "CareerLabel", $"→ {outcome.CareerPath}", 48f,
                new Vector2(0f, -30f), new Vector2(CardSize - 100f, 80f), Color.white);
            career.fontStyle = FontStyles.Bold;

            CreateLabel(root, "SubtitleLabel", outcome.Subtitle, 26f,
                new Vector2(0f, -95f), new Vector2(CardSize - 140f, 50f), new Color(0.82f, 0.86f, 0.92f));

            var stats = CreateLabel(root, "StatsLabel",
                $"総合スコア {outcome.Score} pt\nIF {player.IfScore} / 所持金 {player.Money}万 / メンタル {player.Mental} / 徳 {player.Virtue}",
                24f, new Vector2(0f, -190f), new Vector2(CardSize - 140f, 90f), new Color(0.78f, 0.80f, 0.86f));
            stats.textWrappingMode = TextWrappingModes.Normal;

            CreateLabel(root, "Footer", "#すごろく研究者人生", 24f,
                new Vector2(0f, -CardSize / 2f + 50f), new Vector2(CardSize - 120f, 40f),
                new Color(0.6f, 0.7f, 0.9f));
        }

        private static void CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            JapaneseFontProvider.Apply(tmp);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return tmp;
        }
    }
}
