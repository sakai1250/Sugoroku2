using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace Sugoroku.Editor
{
    public static class SetupStaticTitleScene
    {
        [MenuItem("Tools/シーン作成/TitleScene を静的化 (GUI微調整用)")]
        public static void Setup()
        {
            var scenePath = "Assets/Scenes/TitleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("TitleCanvas not found!");
                return;
            }

            // 1. TitleBackgroundArt の静的配置
            var bgTransform = canvas.transform.Find("TitleBackgroundArt");
            GameObject bgGo;
            if (bgTransform == null)
            {
                bgGo = new GameObject("TitleBackgroundArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
                bgGo.transform.SetParent(canvas.transform, false);
            }
            else
            {
                bgGo = bgTransform.gameObject;
            }

            bgGo.transform.SetAsFirstSibling();
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgRt.pivot = new Vector2(0.5f, 0.5f);

            var bgImg = bgGo.GetComponent<Image>();
            var titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Titles/Title1.png")
                ?? Resources.Load<Sprite>("Titles/Title1");
            if (titleSprite != null)
            {
                bgImg.sprite = titleSprite;
                bgImg.color = Color.white;
                bgImg.raycastTarget = false;
            }

            var fitter = bgGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (titleSprite != null)
            {
                fitter.aspectRatio = titleSprite.rect.width / Mathf.Max(1f, titleSprite.rect.height);
            }
            else
            {
                fitter.aspectRatio = 16f / 9f;
            }

            // 2. TitlePanel の調整
            var titlePanel = canvas.transform.Find("TitlePanel");
            if (titlePanel != null)
            {
                var panelImg = titlePanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.enabled = false; // 背景の青み・暗転を防止
                }

                // TitleLabel の位置調整（上部余白へ）
                var titleLabel = titlePanel.Find("TitleLabel");
                if (titleLabel != null)
                {
                    var tlRt = titleLabel.GetComponent<RectTransform>();
                    tlRt.anchoredPosition = new Vector2(0f, 250f);
                    tlRt.sizeDelta = new Vector2(600f, 70f);
                }

                // StartButton の位置調整
                var startBtn = titlePanel.Find("StartButton");
                if (startBtn != null)
                {
                    var startRt = startBtn.GetComponent<RectTransform>();
                    startRt.anchoredPosition = new Vector2(0f, 110f);
                    startRt.sizeDelta = new Vector2(360f, 52f);
                }

                // DailyChallengeButton
                var dailyBtn = titlePanel.Find("DailyChallengeButton");
                if (dailyBtn == null)
                {
                    dailyBtn = CreateButton(titlePanel, "DailyChallengeButton", "デイリーチャレンジ", new Vector2(0f, 48f), new Vector2(360f, 52f));
                }
                else
                {
                    var dRt = dailyBtn.GetComponent<RectTransform>();
                    dRt.anchoredPosition = new Vector2(0f, 48f);
                    dRt.sizeDelta = new Vector2(360f, 52f);
                }

                // SettingsButton の位置調整
                var settingsBtn = titlePanel.Find("SettingsButton");
                if (settingsBtn != null)
                {
                    var sRt = settingsBtn.GetComponent<RectTransform>();
                    sRt.anchoredPosition = new Vector2(0f, -42f);
                    sRt.sizeDelta = new Vector2(360f, 52f);
                }

                // AchievementsButton の位置調整
                var achBtn = titlePanel.Find("AchievementsButton");
                if (achBtn != null)
                {
                    var aRt = achBtn.GetComponent<RectTransform>();
                    aRt.anchoredPosition = new Vector2(0f, -104f);
                    aRt.sizeDelta = new Vector2(360f, 52f);
                }

                // HelpButton
                var helpBtn = titlePanel.Find("HelpButton");
                if (helpBtn == null)
                {
                    helpBtn = CreateButton(titlePanel, "HelpButton", "遊び方", new Vector2(0f, -164f), new Vector2(360f, 52f));
                }
                else
                {
                    var hRt = helpBtn.GetComponent<RectTransform>();
                    hRt.anchoredPosition = new Vector2(0f, -164f);
                    hRt.sizeDelta = new Vector2(360f, 52f);
                }

                // TitleTagline
                var tagline = titlePanel.Find("TitleTagline");
                if (tagline == null)
                {
                    var go = new GameObject("TitleTagline", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    go.transform.SetParent(titlePanel, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 195f);
                    rt.sizeDelta = new Vector2(840f, 30f);
                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    tmp.text = "サイコロで大学院生活を駆け抜けろ！ 修了してランクSを目指そう";
                    tmp.fontSize = 19f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.26f, 0.30f, 0.42f, 1f);
                }
                else
                {
                    var rt = tagline.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 195f);
                    rt.sizeDelta = new Vector2(840f, 30f);
                }

                // DailyCaption
                var dailyCap = titlePanel.Find("DailyCaption");
                if (dailyCap == null)
                {
                    var go = new GameObject("DailyCaption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    go.transform.SetParent(titlePanel, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 10f);
                    rt.sizeDelta = new Vector2(640f, 18f);
                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    tmp.text = "デイリー: 毎日同じ盤面で、自分の自己ベストに挑戦できるソロモード。";
                    tmp.fontSize = 13f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.35f, 0.40f, 0.52f, 1f);
                }
                else
                {
                    var rt = dailyCap.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 10f);
                    rt.sizeDelta = new Vector2(640f, 18f);
                }

                // BackButton（タイトルへ戻る）
                var backBtn = titlePanel.Find("BackButton");
                if (backBtn == null)
                {
                    backBtn = CreateButton(titlePanel, "BackButton", "タイトルへ戻る", new Vector2(0f, -226f), new Vector2(260f, 44f));
                }
                else
                {
                    var bRt = backBtn.GetComponent<RectTransform>();
                    bRt.anchoredPosition = new Vector2(0f, -226f);
                    bRt.sizeDelta = new Vector2(260f, 44f);
                }
            }

            // 3. TapToStartPanel の静的配置
            var tapPanel = canvas.transform.Find("TapToStartPanel");
            if (tapPanel == null)
            {
                var go = new GameObject("TapToStartPanel", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                tapPanel = go.transform;
            }
            tapPanel.SetSiblingIndex(1); // TitleBackgroundArt の直後
            var tapRt = tapPanel.GetComponent<RectTransform>();
            tapRt.anchorMin = Vector2.zero;
            tapRt.anchorMax = Vector2.one;
            tapRt.offsetMin = tapRt.offsetMax = Vector2.zero;

            // 全画面タップボタン
            var tapBtn = tapPanel.Find("TapToStartButton");
            if (tapBtn == null)
            {
                var btnGo = new GameObject("TapToStartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(tapPanel, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = Vector2.zero;
                btnRt.anchorMax = Vector2.one;
                btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = Color.clear;
            }

            // タイトル
            var tapTitle = tapPanel.Find("TitleLabel");
            if (tapTitle == null)
            {
                EnsureText(tapPanel, "TitleLabel", "大学院生すごろく", 52f, new Vector2(0f, 250f));
                var tmp = tapPanel.Find("TitleLabel")?.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.12f, 0.15f, 0.26f, 1f);
                }
            }

            // タグライン
            var tapTag = tapPanel.Find("TitleTagline");
            if (tapTag == null)
            {
                EnsureText(tapPanel, "TitleTagline", "サイコロで大学院生活を駆け抜けろ！ 修了してランクSを目指そう", 19f, new Vector2(0f, 195f));
                var tmp = tapPanel.Find("TitleTagline")?.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.color = new Color(0.26f, 0.30f, 0.42f, 1f);
            }

            // TAP TO START プロンプト
            var tapPrompt = tapPanel.Find("TapPrompt");
            if (tapPrompt == null)
            {
                EnsureText(tapPanel, "TapPrompt", "— TAP TO START —", 26f, new Vector2(0f, -140f));
                var tmp = tapPanel.Find("TapPrompt")?.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.18f, 0.22f, 0.34f, 1f);
                }
            }

            // 3. SettingsPanel の静的コントロール配置
            var settingsPanel = canvas.transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                EnsureSlider(settingsPanel, "HumanCountSlider", new Vector2(140f, 130f), 1, 4);
                EnsureSlider(settingsPanel, "CpuCountSlider", new Vector2(140f, 80f), 0, 3);
                EnsureText(settingsPanel, "TotalCountText", "合計: 2人", 20f, new Vector2(0f, -80f));

                EnsureText(settingsPanel, "BoardLengthText", "コース: 標準（30マス）", 20f, new Vector2(-120f, 30f));
                EnsureSlider(settingsPanel, "BoardLengthSlider", new Vector2(140f, 30f), 0, 1);

                EnsureText(settingsPanel, "DifficultyText", "難易度: 普通", 20f, new Vector2(-120f, -20f));
                EnsureSlider(settingsPanel, "DifficultySlider", new Vector2(140f, -20f), 0, 2);

                EnsureText(settingsPanel, "BgmVolumeText", "BGM音量: 70", 20f, new Vector2(-120f, -130f));
                EnsureSlider(settingsPanel, "BgmVolumeSlider", new Vector2(140f, -130f), 0, 1);

                EnsureText(settingsPanel, "SeVolumeText", "SE音量: 100", 20f, new Vector2(-120f, -180f));
                EnsureSlider(settingsPanel, "SeVolumeSlider", new Vector2(140f, -180f), 0, 1);

                EnsureText(settingsPanel, "ProfessorText", "指導教員: なし", 20f, new Vector2(-120f, -228f));
                EnsureSlider(settingsPanel, "ProfessorSlider", new Vector2(140f, -228f), 0, 3);

                EnsureText(settingsPanel, "TextSizeText", "文字サイズ: 標準", 20f, new Vector2(-120f, -280f));
                EnsureSlider(settingsPanel, "TextSizeSlider", new Vector2(140f, -280f), 0, 2);

                if (settingsPanel.Find("ReduceMotionButton") == null)
                {
                    CreateButton(settingsPanel, "ReduceMotionButton", "動きを減らす: OFF", new Vector2(0f, -330f), new Vector2(320f, 48f));
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[SetupStaticTitleScene] TitleScene を静的オブジェクト構成に更新・保存しました！</color>");
        }

        private static void EnsureText(Transform parent, string name, string text, float size, Vector2 pos)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(500f, 40f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static void EnsureSlider(Transform parent, string name, Vector2 pos, float min, float max)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260f, 28f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.16f, 0.28f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6f, 6f);
            fillAreaRt.offsetMax = new Vector2(-6f, -6f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.25f, 0.55f, 0.85f);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(34f, 34f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
        }

        private static Transform CreateButton(Transform parent, string name, string labelText, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go.transform;
        }
    }
}
