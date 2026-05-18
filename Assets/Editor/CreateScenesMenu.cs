using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using Sugoroku.Audio;
using Sugoroku.Board;
using Sugoroku.Game;
using Sugoroku.UI;
using Sugoroku.Visual;
using Sugoroku.Network;
using Sugoroku.Data;

namespace Sugoroku.Editor
{
    public static class CreateScenesMenu
    {
        private const string TitleScenePath           = "Assets/Scenes/TitleScene.unity";
        private const string CharacterSelectScenePath = "Assets/Scenes/CharacterSelectScene.unity";
        private const string GameWorldScenePath       = "Assets/Scenes/GameWorldScene.unity";
        private const string GameUIScenePath          = "Assets/Scenes/GameUIScene.unity";
        private const string ResultScenePath          = "Assets/Scenes/ResultScene.unity";
        private const string GameOverScenePath        = "Assets/Scenes/GameOverScene.unity";

        public static void CreateAllScenes()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            CreateTitleScene();
            CreateCharacterSelectScene();
            CreateGameWorldScene();
            CreateGameUIScene();
            CreateResultScene();
            CreateGameOverScene();
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("✅ シーン生成完了: Title / CharacterSelect / GameWorld / GameUI / Result / GameOver");
        }

        private static void CreateTitleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCamera(new Color(0.08f, 0.08f, 0.15f), 6f);

            var canvasGo = CreateCanvasGo("TitleCanvas");
            canvasGo.AddComponent<TitleMenuController>();

            var titlePanel = CreatePanel(canvasGo.transform, "TitlePanel", new Color(0.08f, 0.08f, 0.18f, 0.95f));
            CreateTMP(titlePanel.transform, "TitleLabel", "大学院生すごろく", 48, new Vector2(0, 160));
            CreateButtonGo(titlePanel.transform, "StartButton",        "ゲームスタート", new Vector2(0, 40));
            CreateButtonGo(titlePanel.transform, "SettingsButton",     "設定",           new Vector2(0, -40));
            CreateButtonGo(titlePanel.transform, "AchievementsButton", "実績",           new Vector2(0, -120));

            var settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel", new Color(0.08f, 0.08f, 0.18f, 0.97f));
            settingsPanel.SetActive(false);
            CreateTMP(settingsPanel.transform, "SettingsTitle", "設定", 36, new Vector2(0, 180));
            CreateTMP(settingsPanel.transform, "HumanCountText", "人間プレイヤー: 1", 22, new Vector2(-120, 80));
            CreateTMP(settingsPanel.transform, "CpuCountText",   "CPU: 1", 22, new Vector2(-120, 0));
            CreateTMP(settingsPanel.transform, "TotalCountText", "合計: 2 人", 20, new Vector2(0, -70));
            CreateCountSlider(settingsPanel.transform, "HumanCountSlider", new Vector2(140, 80), 1, 4);
            CreateCountSlider(settingsPanel.transform, "CpuCountSlider", new Vector2(140, 0), 0, 3);
            CreateButtonGo(settingsPanel.transform, "CloseButton", "閉じる", new Vector2(0, -180));

            var achPanel = CreatePanel(canvasGo.transform, "AchievementsPanel", new Color(0.08f, 0.08f, 0.18f, 0.97f));
            achPanel.SetActive(false);
            CreateTMP(achPanel.transform, "AchTitle", "実績", 36, new Vector2(0, 180));
            CreateTMP(achPanel.transform, "AchBody",  "（未実装）", 20, new Vector2(0, 0));
            CreateButtonGo(achPanel.transform, "CloseButton", "閉じる", new Vector2(0, -180));

            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }

        private static void CreateCharacterSelectScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCamera(new Color(0.06f, 0.06f, 0.12f), 6f);

            var canvasGo = CreateCanvasGo("CharacterSelectCanvas");
            var root = CreatePanel(canvasGo.transform, "CharacterSelectRoot", new Color(0.06f, 0.07f, 0.14f, 0.98f));
            var rootImg = root.GetComponent<Image>();
            if (rootImg != null) rootImg.raycastTarget = false;
            root.AddComponent<CharacterSelectController>();
            root.AddComponent<CharacterSelectJuice>();

            CreateButtonGo(root.transform, "BackButton", "← 戻る", new Vector2(-820, 460));
            var backRt = root.transform.Find("BackButton").GetComponent<RectTransform>();
            backRt.sizeDelta = new Vector2(160, 44);

            CreateTMP(root.transform, "ScreenTitle", "キャラ選択", 32, new Vector2(0, 460));

            var cardParent = new GameObject("CardParent");
            cardParent.transform.SetParent(root.transform, false);
            var h = cardParent.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleCenter;
            var cardRt = cardParent.GetComponent<RectTransform>();
            cardRt.anchoredPosition = new Vector2(-280, 80);
            cardRt.sizeDelta = new Vector2(900, 140);

            CreateButtonGo(root.transform, "PrevButton", "◀", new Vector2(-520, 80));
            CreateButtonGo(root.transform, "NextButton", "▶", new Vector2(160, 80));

            var portraitGo = new GameObject("PortraitImage");
            portraitGo.transform.SetParent(root.transform, false);
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.color = Color.white;
            portraitImg.preserveAspect = true;
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchoredPosition = new Vector2(380, 60);
            portraitRt.sizeDelta = new Vector2(200, 200);

            CreateTMP(root.transform, "ClassNameText", "多趣味系 (Hobbyist)", 28, new Vector2(380, 200));
            CreateTMP(root.transform, "TraitNameText", "固有特性: 一旦逃避", 20, new Vector2(380, 140));
            var traitDesc = CreateTMP(root.transform, "TraitDescText", "特性説明", 16, new Vector2(380, 60));
            traitDesc.alignment = TextAlignmentOptions.Left;
            traitDesc.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 100);
            var role = CreateTMP(root.transform, "RoleText", "戦略的役割", 15, new Vector2(380, -40));
            role.alignment = TextAlignmentOptions.Left;
            role.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 80);
            var stats = CreateTMP(root.transform, "StatsText", "初期ステータス", 16, new Vector2(380, -180));
            stats.alignment = TextAlignmentOptions.Left;
            stats.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 140);

            var confirmBtn = CreateButtonGo(root.transform, "ConfirmButton", "このキャラに決める", new Vector2(380, -320));
            confirmBtn.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.35f);

            EditorSceneManager.SaveScene(scene, CharacterSelectScenePath);
        }

        private static void CreateGameWorldScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCamera(new Color(0.1f, 0.15f, 0.1f), 10f, addBoardCam: true);

            var boardGo = new GameObject("Board");
            boardGo.AddComponent<BoardManager>();
            boardGo.AddComponent<LayeredBoardGenerator>();

            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<NetworkSessionHost>();
            gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<TurnManager>();
            var diceRoller = gmGo.AddComponent<DiceRoller>();
            gmGo.AddComponent<EventManager>();
            gmGo.AddComponent<CpuController>();

            CreateBoardDice(diceRoller);

            EditorSceneManager.SaveScene(scene, GameWorldScenePath);
        }

        private static void CreateGameUIScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasGo = CreateCanvasGo("GameUICanvas");
            canvasGo.AddComponent<GameMainLayout>();

            // 上部リソースバー（§3.1 固定ヘッダー）
            var barGo = new GameObject("ResourceBar");
            barGo.transform.SetParent(canvasGo.transform, false);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.05f, 0.06f, 0.12f, 0.92f);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot     = new Vector2(0.5f, 1);
            barRt.sizeDelta = new Vector2(0, 72);
            barRt.anchoredPosition = Vector2.zero;

            CreateBarStat(barGo.transform, "MoneyText",  "所持金: 30 万円",  new Vector2(-600, 0));
            CreateBarStat(barGo.transform, "IfScoreText","IF: 0.0 pt",       new Vector2(-200, 0));
            CreateBarStat(barGo.transform, "MentalText", "メンタル: 50 / 50",new Vector2(200, 0));
            CreateBarStat(barGo.transform, "VirtueText", "徳: 0 pt",         new Vector2(600, 0));

            // HUD ルート（Canvas 配下は RectTransform 必須）
            var hudGo = CreateUiRoot(canvasGo.transform, "GameHUD");
            hudGo.AddComponent<GameHUD>();
            hudGo.AddComponent<HudStatFlash>();
            hudGo.AddComponent<StatJuicePresenter>();
            var hudRt = hudGo.GetComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = hudRt.offsetMax = Vector2.zero;

            CreateHudStat(hudGo.transform, "PlayerNameText", "コマ1（多趣味系）", 18, new Vector2(-820, 420));
            CreateHudStat(hudGo.transform, "TurnStateText", "行動待ち", 14, new Vector2(-820, 390));
            CreateHudStat(hudGo.transform, "GoalDistanceText", "ゴールまで 19マス", 14, new Vector2(-820, 360));
            CreateHudStat(hudGo.transform, "TuitionDistanceText", "学費△5", 14, new Vector2(-820, 330));
            CreateHudStat(hudGo.transform, "SkipTurnsText", "", 14, new Vector2(-820, 300));
            CreateHudStat(hudGo.transform, "IgnoreEventsText", "", 14, new Vector2(-820, 270));

            var mentalSliderGo = new GameObject("MentalSlider");
            mentalSliderGo.transform.SetParent(hudGo.transform, false);
            var slider = mentalSliderGo.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 50;
            slider.value = 50;
            var msRt = mentalSliderGo.GetComponent<RectTransform>();
            msRt.anchoredPosition = new Vector2(-820, 240);
            msRt.sizeDelta = new Vector2(200, 12);
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(mentalSliderGo.transform, false);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.75f, 0.95f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            var actionPanel = new GameObject("ActionPanel");
            actionPanel.transform.SetParent(canvasGo.transform, false);
            var actionRt = actionPanel.AddComponent<RectTransform>();
            actionRt.anchorMin = actionRt.anchorMax = new Vector2(1f, 0f);
            actionRt.pivot = new Vector2(1f, 0f);
            actionRt.anchoredPosition = new Vector2(-24f, 24f);
            actionRt.sizeDelta = new Vector2(240f, 140f);
            var actionV = actionPanel.AddComponent<VerticalLayoutGroup>();
            actionV.spacing = 10f;
            actionV.childAlignment = TextAnchor.LowerRight;
            actionV.childControlWidth = false;
            actionV.childControlHeight = false;

            var rollBtn = CreateButtonGo(actionPanel.transform, "RollButton", "ダイスを振る", Vector2.zero);
            var skillBtn = CreateButtonGo(actionPanel.transform, "SkillButton", "ワザ", Vector2.zero);
            var menuBtn = CreateButtonGo(canvasGo.transform, "MenuButton", "メニュー", new Vector2(900, 480));
            var menuRt = menuBtn.GetComponent<RectTransform>();
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(1, 1);
            menuRt.anchoredPosition = new Vector2(-60, -40);
            menuRt.sizeDelta = new Vector2(120, 44);

            var diceGo = new GameObject("DiceResult");
            diceGo.transform.SetParent(actionPanel.transform, false);
            var diceTmp = diceGo.AddComponent<TextMeshProUGUI>();
            diceTmp.text = "";
            diceTmp.fontSize = 48;
            diceTmp.alignment = TextAlignmentOptions.Center;
            ApplyJapaneseFont(diceTmp);
            var diceRt = diceGo.GetComponent<RectTransform>();
            diceRt.anchorMin = diceRt.anchorMax = new Vector2(0.5f, 0.5f);
            diceRt.anchoredPosition = Vector2.zero;
            diceRt.sizeDelta = new Vector2(200, 100);

            var diceIconGo = new GameObject("DiceIcon");
            diceIconGo.transform.SetParent(diceGo.transform, false);
            diceIconGo.AddComponent<DiceHudAnimator>();
            var diceIconImg = diceIconGo.AddComponent<Image>();
            var iconSp = KenneyAssets.GetDiceHudIcon(6);
            if (iconSp != null) diceIconImg.sprite = iconSp;
            diceIconImg.preserveAspect = true;
            var diceIconRt = diceIconGo.GetComponent<RectTransform>();
            diceIconRt.anchorMin = diceIconRt.anchorMax = new Vector2(0f, 0.5f);
            diceIconRt.pivot = new Vector2(0.5f, 0.5f);
            diceIconRt.anchoredPosition = new Vector2(36f, 0f);
            diceIconRt.sizeDelta = new Vector2(72f, 72f);

            // ポーズ / ステータス詳細
            var statusPanel = CreatePanel(canvasGo.transform, "StatusPanel", new Color(0.04f, 0.05f, 0.12f, 0.97f));
            statusPanel.AddComponent<PauseMenuUI>();
            statusPanel.SetActive(false);
            CreateTMP(statusPanel.transform, "StatusTitle", "ステータス詳細", 32, new Vector2(0, 220));
            var breakdown = CreateTMP(statusPanel.transform, "ScoreBreakdownText",
                "【現在の総合スコア計算】\n" + ScoreCalculator.FormulaLine, 15, new Vector2(0, 0));
            breakdown.alignment = TextAlignmentOptions.Left;
            breakdown.textWrappingMode = TextWrappingModes.Normal;
            var breakdownRt = breakdown.GetComponent<RectTransform>();
            breakdownRt.sizeDelta = new Vector2(600, 360);
            breakdownRt.anchoredPosition = new Vector2(0, -20);
            CreateButtonGo(statusPanel.transform, "ResumeButton", "再開", new Vector2(0, -200));
            CreateButtonGo(statusPanel.transform, "TitleButton", "タイトルへ", new Vector2(0, -270));

            // イベントモーダル
            var modalPanel = CreatePanel(canvasGo.transform, "EventModalPanel", new Color(0.05f, 0.05f, 0.15f, 0.97f));
            modalPanel.AddComponent<CanvasGroup>();
            modalPanel.AddComponent<EventModalUI>();
            modalPanel.SetActive(false);
            var modalHeader = new GameObject("ModalHeader");
            modalHeader.transform.SetParent(modalPanel.transform, false);
            var modalHeaderRt = modalHeader.AddComponent<RectTransform>();
            modalHeaderRt.anchoredPosition = new Vector2(0, 200);
            modalHeaderRt.sizeDelta = new Vector2(720, 44);
            CreateTMP(modalHeader.transform, "ModalTitle", "イベントタイトル", 26, new Vector2(-180, 0));
            CreateTMP(modalPanel.transform, "ModalTags", "[Academic]", 12, new Vector2(300, 180));
            var modalDesc = CreateTMP(modalPanel.transform, "ModalDescription", "イベント説明文", 14, new Vector2(0, 80));
            modalDesc.alignment = TextAlignmentOptions.TopLeft;
            modalDesc.textWrappingMode = TextWrappingModes.Normal;
            modalDesc.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 110);
            var choiceParent = new GameObject("ChoiceButtonParent");
            choiceParent.transform.SetParent(modalPanel.transform, false);
            var vLayout = choiceParent.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10;
            vLayout.childForceExpandWidth = true;
            var cpRt = choiceParent.GetComponent<RectTransform>();
            cpRt.anchoredPosition = new Vector2(0, -80);
            cpRt.sizeDelta = new Vector2(700, 240);

            // ログ
            var logGo = new GameObject("LogText");
            logGo.transform.SetParent(canvasGo.transform, false);
            var logTmp = logGo.AddComponent<TextMeshProUGUI>();
            logTmp.fontSize = 13;
            ApplyJapaneseFont(logTmp);
            var logRt = logGo.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0, 0);
            logRt.anchorMax = new Vector2(0.35f, 0.2f);
            logRt.offsetMin = new Vector2(12, 12);
            logRt.offsetMax = new Vector2(-12, -12);

            canvasGo.AddComponent<UiSoundPlayer>();
            var floatGo = new GameObject("FloatingText");
            floatGo.transform.SetParent(canvasGo.transform, false);
            floatGo.AddComponent<FloatingTextUI>();

            WireSugorokuScenesEditor.WireGameUIScene();
            EditorSceneManager.SaveScene(scene, GameUIScenePath);
        }

        private static void CreateResultScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCamera(new Color(0.05f, 0.08f, 0.12f), 6f);

            var canvasGo = CreateCanvasGo("ResultCanvas");
            var panel = CreatePanel(canvasGo.transform, "ResultRoot", new Color(0.05f, 0.07f, 0.14f, 0.98f));
            panel.AddComponent<ResultSceneController>();

            CreateTMP(panel.transform, "ResultTitle", "修了発表", 44, new Vector2(0, 380));
            var body = CreateTMP(panel.transform, "ResultBody", "", 18, new Vector2(0, 0));
            body.alignment = TextAlignmentOptions.TopLeft;
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 500);
            CreateButtonGo(panel.transform, "TitleButton", "タイトルへ", new Vector2(0, -380));

            EditorSceneManager.SaveScene(scene, ResultScenePath);
        }

        private static void CreateGameOverScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCamera(new Color(0.02f, 0.02f, 0.04f), 6f);

            var canvasGo = CreateCanvasGo("GameOverCanvas");
            var panel = CreatePanel(canvasGo.transform, "GameOverRoot", new Color(0.02f, 0.02f, 0.025f, 1f));
            panel.AddComponent<GameOverSceneController>();
            panel.AddComponent<GameOverSceneJuice>();

            var accent = CreatePanel(panel.transform, "AccentPanel", new Color(0.55f, 0.1f, 0.1f, 0.9f));
            var accentRt = accent.GetComponent<RectTransform>();
            accentRt.anchorMin = accentRt.anchorMax = new Vector2(0.5f, 0.5f);
            accentRt.sizeDelta = new Vector2(700, 320);
            accentRt.anchoredPosition = new Vector2(0, 40);

            CreateTMP(panel.transform, "GameOverTitle", "【破産】学費・生活費未納による強制退学", 32, new Vector2(0, 300));
            var body = CreateTMP(panel.transform, "GameOverBody", "", 18, new Vector2(0, -60));
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 280);
            CreateButtonGo(panel.transform, "TitleButton", "タイトルへ", new Vector2(0, -320));

            EditorSceneManager.SaveScene(scene, GameOverScenePath);
        }

        private static void CreateBoardDice(DiceRoller diceRoller)
        {
            var diceGo = new GameObject("Dice");
            diceGo.transform.position = new Vector3(9f, 1f, 0f);

            var sr = diceGo.AddComponent<SpriteRenderer>();
            var faces = KenneyAssets.LoadDiceFaces("dieWhite");
            if (faces.Length >= 6) sr.sprite = faces[5];

            var box = diceGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.1f, 1.1f);

            var boardDice = diceGo.AddComponent<BoardDice>();

            var so = new SerializedObject(diceRoller);
            var prop = so.FindProperty("_boardDice");
            if (prop != null)
            {
                prop.objectReferenceValue = boardDice;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupCamera(Color bg, float size, bool addBoardCam = false)
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            if (addBoardCam) camGo.AddComponent<BoardCameraController>();
        }

        private static GameObject CreateCanvasGo(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<KenneyUiBootstrap>();

            var canvasRt = go.GetComponent<RectTransform>();
            canvasRt.localScale = Vector3.one;
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = canvasRt.offsetMax = Vector2.zero;

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            return go;
        }

        private static GameObject CreateUiRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            ApplyJapaneseFont(tmp);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600, 60);
            return tmp;
        }

        private static GameObject CreateButtonGo(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var btnSprite = KenneyAssets.LoadSprite(KenneyAssets.UiPack.ButtonDepth);
            if (btnSprite != null) { img.sprite = btnSprite; img.color = Color.white; }
            else img.color = new Color(0.25f, 0.35f, 0.55f);
            go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280, 55);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            ApplyJapaneseFont(tmp);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return go;
        }

        private static Slider CreateCountSlider(Transform parent, string name, Vector2 pos, int min, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260, 28);

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.16f, 0.28f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6, 6);
            fillAreaRt.offsetMax = new Vector2(-6, -6);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.55f, 0.85f);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(18, 18);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = min;
            return slider;
        }

        private static void CreateBarStat(Transform parent, string name, string text, Vector2 pos)
        {
            var tmp = CreateTMP(parent, name, text, 20, pos);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 40);
        }

        private static void CreateHudStat(Transform parent, string name, string text, float size, Vector2 pos)
        {
            var tmp = CreateTMP(parent, name, text, size, pos);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 28);
        }

        private static TMP_FontAsset _cachedJpFont;

        private static void ApplyJapaneseFont(TextMeshProUGUI tmp)
        {
            if (_cachedJpFont == null)
            {
                _cachedJpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
                if (_cachedJpFont == null)
                {
                    foreach (var guid in AssetDatabase.FindAssets("NotoSansJP t:TMP_FontAsset"))
                    {
                        _cachedJpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                        if (_cachedJpFont != null) break;
                    }
                }
                if (_cachedJpFont == null)
                    _cachedJpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
            if (_cachedJpFont != null) tmp.font = _cachedJpFont;
        }

        private static void AddScenesToBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath,           true),
                new EditorBuildSettingsScene(CharacterSelectScenePath, true),
                new EditorBuildSettingsScene(GameWorldScenePath,       true),
                new EditorBuildSettingsScene(GameUIScenePath,          true),
                new EditorBuildSettingsScene(ResultScenePath,          true),
                new EditorBuildSettingsScene(GameOverScenePath,        true),
            };
        }
    }
}
