using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sugoroku.Game;
using Sugoroku.Data;
using Sugoroku.Board;
using Sugoroku.Audio;

namespace Sugoroku.UI
{
    // SerializeField への手動アサイン不要：子オブジェクト名で自動解決
    public class TitleMenuController : MonoBehaviour
    {
        private GameObject  _tapToStartPanel;
        private GameObject  _titlePanel;
        private GameObject  _settingsPanel;
        private GameObject  _achievementsPanel;
        private AchievementsPanelController _achievementsPanelController;
        private Image       _bgImage;
        private Coroutine   _bgFadeRoutine;
        private Coroutine   _tapPulseRoutine;
        private Slider      _humanSlider;
        private Slider      _cpuSlider;
        private Slider      _boardSlider;
        private Slider      _difficultySlider;
        private Slider      _bgmVolumeSlider;
        private Slider      _seVolumeSlider;
        private Slider      _professorSlider;
        private Slider      _textSizeSlider;
        private Button      _reduceMotionToggle;
        private float _bgmVolume = 0.7f;
        private float _seVolume  = 1.0f;
        private ProfessorType _professor = ProfessorType.None;

        private int _humanCount = 1;
        private int _cpuCount   = 1;
        private int _boardCells = (int)BoardLengthOption.Standard;
        private GameDifficulty _difficulty = GameDifficulty.Normal;
        private GameObject _lastTitleSelection;
        private readonly System.Collections.Generic.Dictionary<int, float> _baseFontSizes = new();

        private void Awake()
        {
            JapaneseFontProvider.WarmUp();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                JapaneseFontProvider.ApplyAllInCanvas(canvas);
                TitleSceneDecorations.Ensure(canvas.transform);
            }
        }

        private void Start()
        {
            GameSession.LoadSettings();
            _humanCount  = GameSession.HumanCount;
            _cpuCount    = GameSession.CpuCount;
            _boardCells  = GameSession.BoardCellCount;
            _difficulty  = GameSession.Difficulty;
            _bgmVolume   = GameSession.BgmVolume;
            _seVolume    = GameSession.SeVolume;
            _professor   = GameSession.SelectedProfessor;

            ResolveBgImage();
            _tapToStartPanel   = FindChild("TapToStartPanel");
            _titlePanel        = FindChild("TitlePanel");
            _settingsPanel     = FindChild("SettingsPanel");
            _achievementsPanel = FindChild("AchievementsPanel");
            if (_achievementsPanel != null)
            {
                _achievementsPanelController = _achievementsPanel.GetComponent<AchievementsPanelController>();
                if (_achievementsPanelController == null)
                    _achievementsPanelController = _achievementsPanel.AddComponent<AchievementsPanelController>();
            }

            EnsureTapToStartPanel();
            EnsureSettingsControls();
            EnsureDailyChallengeButton();
            EnsureHelpButton();
            EnsureBackButton();
            EnsureTitleTagline();
            EnsureDailyCaption();
            ApplySafeLayout();
            WireButtons();
            RefreshSettingsLabels();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) KenneyUiStyler.StyleCanvas(canvas);
            ApplyScreenChrome();
            ShowTapToStart();
            ApplyUiTextScaleToAll();
            GameAudioController.Instance?.PlayTitleBgm();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (_tapToStartPanel != null && _tapToStartPanel.activeSelf)
            {
                bool pressed =
                    (kb != null && kb.anyKey.wasPressedThisFrame) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                    (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
                if (pressed)
                {
                    OnTapToStart();
                }
            }
            else if (_titlePanel != null && _titlePanel.activeSelf)
            {
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    ShowTapToStart();
                }
            }
        }

        private void EnsureSettingsControls()
        {
            if (_settingsPanel == null) return;
            var panel = _settingsPanel.transform;

            _humanSlider = FindChildComponent<Slider>("SettingsPanel/HumanCountSlider");
            if (_humanSlider == null)
            {
                _humanSlider = CreateCountSlider(panel, "HumanCountSlider", new Vector2(140f, 130f), 1, 4);
                SetText("SettingsPanel/HumanCountText", $"人間プレイヤー: {_humanCount}");
                var humanTextRt = panel.Find("HumanCountText")?.GetComponent<RectTransform>();
                if (humanTextRt != null) humanTextRt.anchoredPosition = new Vector2(-120f, 130f);
            }

            _cpuSlider = FindChildComponent<Slider>("SettingsPanel/CpuCountSlider");
            if (_cpuSlider == null)
            {
                _cpuSlider = CreateCountSlider(panel, "CpuCountSlider", new Vector2(140f, 80f), 0, 3);
                SetText("SettingsPanel/CpuCountText", $"CPU: {_cpuCount}");
                var cpuTextRt = panel.Find("CpuCountText")?.GetComponent<RectTransform>();
                if (cpuTextRt != null) cpuTextRt.anchoredPosition = new Vector2(-120f, 80f);
            }

            _boardSlider = FindChildComponent<Slider>("SettingsPanel/BoardLengthSlider");
            if (_boardSlider == null)
            {
                _boardSlider = CreateCountSlider(panel, "BoardLengthSlider", new Vector2(140f, 30f), 0, 1);
                SetText("SettingsPanel/BoardLengthText", $"コース: {DifficultyRules.GetBoardLengthLabel(_boardCells)}");
                var boardTextRt = panel.Find("BoardLengthText")?.GetComponent<RectTransform>();
                if (boardTextRt != null) boardTextRt.anchoredPosition = new Vector2(-120f, 30f);
            }

            _difficultySlider = FindChildComponent<Slider>("SettingsPanel/DifficultySlider");
            if (_difficultySlider == null)
            {
                _difficultySlider = CreateCountSlider(panel, "DifficultySlider", new Vector2(140f, -20f), 0, 2);
                SetText("SettingsPanel/DifficultyText", $"難易度: {DifficultyRules.GetLabel(_difficulty)}");
                var diffTextRt = panel.Find("DifficultyText")?.GetComponent<RectTransform>();
                if (diffTextRt != null) diffTextRt.anchoredPosition = new Vector2(-120f, -20f);
            }

            _bgmVolumeSlider = FindChildComponent<Slider>("SettingsPanel/BgmVolumeSlider");
            if (_bgmVolumeSlider == null)
            {
                _bgmVolumeSlider = CreateCountSlider(panel, "BgmVolumeSlider", new Vector2(140f, -130f), 0, 1);
                _bgmVolumeSlider.wholeNumbers = false;
                CreateTMP(panel, "BgmVolumeText", $"BGM音量: {Mathf.RoundToInt(_bgmVolume * 100f)}", 20, new Vector2(-120f, -130f));
            }

            _seVolumeSlider = FindChildComponent<Slider>("SettingsPanel/SeVolumeSlider");
            if (_seVolumeSlider == null)
            {
                _seVolumeSlider = CreateCountSlider(panel, "SeVolumeSlider", new Vector2(140f, -180f), 0, 1);
                _seVolumeSlider.wholeNumbers = false;
                CreateTMP(panel, "SeVolumeText", $"SE音量: {Mathf.RoundToInt(_seVolume * 100f)}", 20, new Vector2(-120f, -180f));
            }

            _professorSlider = FindChildComponent<Slider>("SettingsPanel/ProfessorSlider");
            if (_professorSlider == null)
            {
                _professorSlider = CreateCountSlider(panel, "ProfessorSlider", new Vector2(140f, -228f), 0, 3);
                CreateTMP(panel, "ProfessorText", $"指導教員: {_professor.DisplayName()}", 20, new Vector2(-120f, -228f));
            }

            // 教授名と効果のテキスト設定（既存のTransformは尊重）
            var profText = panel.Find("ProfessorText")?.GetComponent<TextMeshProUGUI>();
            if (profText != null)
            {
                profText.textWrappingMode = TextWrappingModes.Normal;
                ApplyJapaneseFont(profText);
            }

            _textSizeSlider = FindChildComponent<Slider>("SettingsPanel/TextSizeSlider");
            if (_textSizeSlider == null)
            {
                _textSizeSlider = CreateCountSlider(panel, "TextSizeSlider", new Vector2(140f, -280f), 0, 2);
                CreateTMP(panel, "TextSizeText", "文字サイズ: 標準", 20, new Vector2(-120f, -280f));
            }

            _reduceMotionToggle = FindChildComponent<Button>("SettingsPanel/ReduceMotionButton");
            if (_reduceMotionToggle == null)
            {
                var go = new GameObject("ReduceMotionButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panel, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -330f);
                rt.sizeDelta = new Vector2(320f, UiSafeLayout.MinimumButtonHeight);
                var label = CreateTMP(go.transform, "Label", "動きを減らす: OFF", 20, new Vector2(0f, 0f));
                var labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
                _reduceMotionToggle = go.GetComponent<Button>();
                GameUiChrome.ApplyButton(_reduceMotionToggle, primary: false);
            }

            if (panel.Find("TotalCountText") == null)
            {
                var total = CreateTMP(panel, "TotalCountText", "合計: 2人", 20, new Vector2(0f, -80f));
                total.color = new Color(0.75f, 0.8f, 0.95f);
            }
        }

        private Slider CreateCountSlider(Transform parent, string name, Vector2 pos, int min, int max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260f, 28f);

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
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

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
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

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            // タップ領域の拡大: 26px ではモバイルで掴みにくいため 34px へ
            handleRt.sizeDelta = new Vector2(34f, 34f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            return slider;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            ApplyJapaneseFont(tmp);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(500f, 40f);
            return tmp;
        }

        private void EnsureDailyChallengeButton()
        {
            if (_titlePanel == null) return;
            var existing = _titlePanel.transform.Find("DailyChallengeButton");
            if (existing != null)
            {
                var label = existing.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) ApplyJapaneseFont(label);
                return;
            }

            var startRt = _titlePanel.transform.Find("StartButton")?.GetComponent<RectTransform>();
            var pos = new Vector2(0f, 28f);

            var go = new GameObject("DailyChallengeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_titlePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = startRt != null ? startRt.sizeDelta : new Vector2(360f, 56f);
            rt.anchoredPosition = pos;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "デイリーチャレンジ";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            ApplyJapaneseFont(tmp);

            var btn = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: false);
        }

        private void EnsureHelpButton()
        {
            if (_titlePanel == null) return;
            var existing = _titlePanel.transform.Find("HelpButton");
            if (existing != null)
            {
                var label = existing.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) ApplyJapaneseFont(label);
                return;
            }

            var go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_titlePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 56f);
            rt.anchoredPosition = new Vector2(0f, -176f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "遊び方";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            ApplyJapaneseFont(tmp);

            var btn = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: false);
        }

        private void EnsureTitleTagline()
        {
            if (_titlePanel == null) return;
            var existing = _titlePanel.transform.Find("TitleTagline");
            if (existing != null)
            {
                var tmpExisting = existing.GetComponent<TextMeshProUGUI>();
                if (tmpExisting != null) ApplyJapaneseFont(tmpExisting);
                return;
            }
            var tmp = CreateTMP(_titlePanel.transform, "TitleTagline",
                "サイコロで大学院生活を駆け抜けろ！ 修了してランクSを目指そう", 20, new Vector2(0f, 184f));
            tmp.color = new Color(0.78f, 0.82f, 0.90f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(840f, 32f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void EnsureDailyCaption()
        {
            if (_titlePanel == null) return;
            var existing = _titlePanel.transform.Find("DailyCaption");
            if (existing != null)
            {
                var tmpExisting = existing.GetComponent<TextMeshProUGUI>();
                if (tmpExisting != null) ApplyJapaneseFont(tmpExisting);
                return;
            }
            var tmp = CreateTMP(_titlePanel.transform, "DailyCaption",
                "デイリー: 毎日同じ盤面で、自分の自己ベストに挑戦できるソロモード。", 13, new Vector2(0f, -20f));
            tmp.color = new Color(0.66f, 0.70f, 0.80f);
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(640f, 18f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void EnsureTapToStartPanel()
        {
            if (_tapToStartPanel != null)
            {
                var prompt = _tapToStartPanel.transform.Find("TapPrompt")?.GetComponent<TextMeshProUGUI>();
                if (prompt != null) ApplyJapaneseFont(prompt);
                return;
            }

            var parent = transform.parent != null ? transform.parent : transform;
            _tapToStartPanel = parent.Find("TapToStartPanel")?.gameObject;
            if (_tapToStartPanel != null)
            {
                var prompt = _tapToStartPanel.transform.Find("TapPrompt")?.GetComponent<TextMeshProUGUI>();
                if (prompt != null) ApplyJapaneseFont(prompt);
                return;
            }

            // シーンにない場合のフォールバック動的生成
            var go = new GameObject("TapToStartPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var btnGo = new GameObject("TapToStartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = Color.clear;

            CreateTMP(go.transform, "TitleLabel", "大学院生すごろく", 52f, new Vector2(0f, 250f));
            CreateTMP(go.transform, "TitleTagline", "サイコロで大学院生活を駆け抜けろ！ 修了してランクSを目指そう", 19f, new Vector2(0f, 195f));
            var pTmp = CreateTMP(go.transform, "TapPrompt", "— TAP TO START —", 26f, new Vector2(0f, -140f));
            pTmp.fontStyle = FontStyles.Bold;

            _tapToStartPanel = go;
        }

        private void EnsureBackButton()
        {
            if (_titlePanel == null) return;
            var existing = _titlePanel.transform.Find("BackButton");
            if (existing != null)
            {
                var label = existing.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) ApplyJapaneseFont(label);
                return;
            }

            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_titlePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -226f);
            rt.sizeDelta = new Vector2(260f, 44f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "タイトルへ戻る";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            ApplyJapaneseFont(tmp);

            var btn = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: false);
        }

        private void WireButtons()
        {
            WireButton("TapToStartPanel/TapToStartButton", OnTapToStart);
            WireButton("TitlePanel/BackButton",           ShowTapToStart);
            WireButton("TitlePanel/StartButton",          OnStartGame);
            WireButton("TitlePanel/DailyChallengeButton", OnStartDailyChallenge);
            WireButton("TitlePanel/SettingsButton",       () => ShowPanel(_settingsPanel));
            WireButton("TitlePanel/HelpButton",           () => HelpPanelUI.Open());
            WireButton("TitlePanel/AchievementsButton",   () =>
            {
                _achievementsPanelController?.Refresh();
                ShowPanel(_achievementsPanel);
            });
            WireButton("SettingsPanel/CloseButton",       () => ShowPanel(_titlePanel));
            WireButton("AchievementsPanel/CloseButton",   () => ShowPanel(_titlePanel));

            if (_humanSlider != null)
            {
                _humanSlider.value = _humanCount;
                _humanSlider.onValueChanged.RemoveAllListeners();
                _humanSlider.onValueChanged.AddListener(v =>
                {
                    _humanCount = (int)v;
                    ClampCounts();
                    RefreshSettingsLabels();
                });
            }

            if (_cpuSlider != null)
            {
                _cpuSlider.value = _cpuCount;
                _cpuSlider.onValueChanged.RemoveAllListeners();
                _cpuSlider.onValueChanged.AddListener(v =>
                {
                    _cpuCount = (int)v;
                    ClampCounts();
                    RefreshSettingsLabels();
                });
            }

            if (_boardSlider != null)
            {
                _boardSlider.value = BoardCellsToSlider(_boardCells);
                _boardSlider.onValueChanged.RemoveAllListeners();
                _boardSlider.onValueChanged.AddListener(v =>
                {
                    _boardCells = SliderToBoardCells((int)v);
                    RefreshSettingsLabels();
                });
            }

            if (_difficultySlider != null)
            {
                _difficultySlider.value = (int)_difficulty;
                _difficultySlider.onValueChanged.RemoveAllListeners();
                _difficultySlider.onValueChanged.AddListener(v =>
                {
                    _difficulty = (GameDifficulty)(int)v;
                    RefreshSettingsLabels();
                });
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.value = _bgmVolume;
                _bgmVolumeSlider.onValueChanged.RemoveAllListeners();
                _bgmVolumeSlider.onValueChanged.AddListener(v =>
                {
                    _bgmVolume = v;
                    GameSession.BgmVolume = v;
                    GameSession.SaveSettings();
                    GameAudioController.Instance?.SetBgmVolume(v);
                    RefreshSettingsLabels();
                });
            }

            if (_seVolumeSlider != null)
            {
                _seVolumeSlider.value = _seVolume;
                _seVolumeSlider.onValueChanged.RemoveAllListeners();
                _seVolumeSlider.onValueChanged.AddListener(v =>
                {
                    _seVolume = v;
                    GameSession.SeVolume = v;
                    GameSession.SaveSettings();
                    GameAudioController.Instance?.SetSeVolume(v);
                    RefreshSettingsLabels();
                });
            }

            if (_professorSlider != null)
            {
                _professorSlider.value = (int)_professor;
                _professorSlider.onValueChanged.RemoveAllListeners();
                _professorSlider.onValueChanged.AddListener(v =>
                {
                    _professor = (ProfessorType)(int)v;
                    GameSession.SelectedProfessor = _professor;
                    GameSession.SaveSettings();
                    RefreshSettingsLabels();
                });
            }

            if (_textSizeSlider != null)
            {
                _textSizeSlider.value = TextScaleToSlider(GameSession.UiTextScale);
                _textSizeSlider.onValueChanged.RemoveAllListeners();
                _textSizeSlider.onValueChanged.AddListener(v =>
                {
                    GameSession.UiTextScale = SliderToTextScale((int)v);
                    GameSession.SaveSettings();
                    RefreshSettingsLabels();
                    ApplyUiTextScaleToAll();
                });
            }

            if (_reduceMotionToggle != null)
            {
                _reduceMotionToggle.onClick.RemoveAllListeners();
                _reduceMotionToggle.onClick.AddListener(() =>
                {
                    GameSession.ReduceMotion = !GameSession.ReduceMotion;
                    GameSession.SaveSettings();
                    RefreshSettingsLabels();
                });
            }
        }

        private static string ProfessorEffectSummary(ProfessorType p) => p switch
        {
            ProfessorType.Laissez       => "IF（研究実績）が10%減 / メンタルダメージ15%減",
            ProfessorType.DrillSergeant => "IF（研究実績）が15%増 / メンタルダメージ15%増",
            ProfessorType.Virtuous      => "徳（善行ポイント）の獲得量が20%増",
            _                           => "なし"
        };

        private static int TextScaleToSlider(float scale) =>
            scale <= 0.95f ? 0 : (scale >= 1.15f ? 2 : 1);

        private static float SliderToTextScale(int slider) => slider switch
        {
            0 => 0.85f,
            2 => 1.25f,
            _ => 1f
        };

        private static string TextSizeLabel(float scale) => scale <= 0.95f ? "小"
            : scale >= 1.15f ? "大"
            : "標準";

        private static int BoardCellsToSlider(int cells) =>
            cells == (int)BoardLengthOption.ExtraLong ? 1 : 0;

        private static int SliderToBoardCells(int slider) =>
            slider == 1 ? (int)BoardLengthOption.ExtraLong : (int)BoardLengthOption.Standard;

        private void ClampCounts()
        {
            _humanCount = Mathf.Clamp(_humanCount, 1, GameConfig.MaxPlayers);
            _cpuCount   = Mathf.Clamp(_cpuCount, 0, GameConfig.MaxPlayers - 1);
            if (_humanCount + _cpuCount > GameConfig.MaxPlayers)
                _cpuCount = GameConfig.MaxPlayers - _humanCount;
            if (_humanCount + _cpuCount < 1)
                _cpuCount = 0;

            if (_humanSlider != null) _humanSlider.SetValueWithoutNotify(_humanCount);
            if (_cpuSlider != null)   _cpuSlider.SetValueWithoutNotify(_cpuCount);
            if (_boardSlider != null) _boardSlider.SetValueWithoutNotify(BoardCellsToSlider(_boardCells));
            if (_difficultySlider != null) _difficultySlider.SetValueWithoutNotify((int)_difficulty);
        }

        private void RefreshSettingsLabels()
        {
            SetText("SettingsPanel/HumanCountText", $"人間プレイヤー: {_humanCount}");
            SetText("SettingsPanel/CpuCountText",   $"CPU: {_cpuCount}");
            SetText("SettingsPanel/BoardLengthText", $"コース: {DifficultyRules.GetBoardLengthLabel(_boardCells)}");
            SetText("SettingsPanel/DifficultyText", $"難易度: {DifficultyRules.GetLabel(_difficulty)}");
            SetText("SettingsPanel/TotalCountText", $"合計: {_humanCount + _cpuCount} 人（最大 {GameConfig.MaxPlayers}）");
            SetText("SettingsPanel/BgmVolumeText", $"BGM音量: {Mathf.RoundToInt(_bgmVolume * 100f)}");
            SetText("SettingsPanel/SeVolumeText", $"SE音量: {Mathf.RoundToInt(_seVolume * 100f)}");
            SetText("SettingsPanel/ProfessorText", $"指導教員: {_professor.DisplayName()}\n{ProfessorEffectSummary(_professor)}");
            SetText("SettingsPanel/TextSizeText", $"文字サイズ: {TextSizeLabel(GameSession.UiTextScale)}");
            SetText("SettingsPanel/ReduceMotionButton/Label", $"動きを減らす: {(GameSession.ReduceMotion ? "ON" : "OFF")}");
            UpdateStartButtonLabel();
        }

        /// <summary>誤ってソロ開始しないよう、スタートボタンに現在の人数構成を常時表示する。</summary>
        private void UpdateStartButtonLabel()
        {
            var label = _titlePanel?.transform.Find("StartButton")?.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;
            label.text = _humanCount > 1
                ? $"みんなではじめる（人間{_humanCount}・CPU{_cpuCount}）"
                : $"はじめる（人間1・CPU{_cpuCount}）";
        }

        /// <summary>文字サイズ設定をタイトル画面全体のTMPへ即時反映（初回は基準値を記録）。</summary>
        private void ApplyUiTextScaleToAll()
        {
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp == null) continue;
                int id = tmp.GetInstanceID();
                if (!_baseFontSizes.TryGetValue(id, out float baseSize))
                {
                    baseSize = tmp.fontSize;
                    _baseFontSizes[id] = baseSize;
                }
                if (tmp.enableAutoSizing) continue;
                tmp.fontSize = baseSize * GameSession.UiTextScale;
            }
        }

        private void ApplySessionSettings()
        {
            GameSession.HumanCount     = _humanCount;
            GameSession.CpuCount       = _cpuCount;
            GameSession.BoardCellCount = _boardCells;
            GameSession.Difficulty     = _difficulty;
            GameSession.SaveSettings();
            BoardLayoutGenerator.Invalidate();
        }

        private void OnStartGame()
        {
            ClampCounts();
            GameSession.IsDailyChallenge = false;
            ApplySessionSettings();
            GameSession.EnsureHumanCharacters();
            SceneManager.LoadScene("CharacterSelectScene");
        }

        private void OnStartDailyChallenge()
        {
            GameSession.IsDailyChallenge = true;
            GameSession.DailySeed        = GameSession.ComputeDailySeed(System.DateTime.UtcNow);
            GameSession.HumanCount       = 1;
            GameSession.CpuCount         = 0;
            GameSession.ApplyDailyChallengeDefaults();
            BoardLayoutGenerator.Invalidate();
            GameSession.EnsureHumanCharacters();
            SceneManager.LoadScene("CharacterSelectScene");
        }

        public void ShowTapToStart()
        {
            if (_tapPulseRoutine != null) StopCoroutine(_tapPulseRoutine);
            if (_tapToStartPanel != null) _tapToStartPanel.SetActive(true);
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_achievementsPanel != null) _achievementsPanel.SetActive(false);

            // 背景pngの不透明度を100%（鮮明なイラスト）に戻す
            FadeBackgroundAlpha(1.0f, 0.35f);

            var prompt = _tapToStartPanel?.transform.Find("TapPrompt")?.GetComponent<TextMeshProUGUI>();
            if (prompt != null)
            {
                _tapPulseRoutine = StartCoroutine(PulseTapPrompt(prompt));
            }
        }

        public void OnTapToStart()
        {
            if (_tapPulseRoutine != null)
            {
                StopCoroutine(_tapPulseRoutine);
                _tapPulseRoutine = null;
            }

            // メニュー画面とキャラ設定画面を統合：Tap to Start から直接キャラ設定画面へ遷移
            Audio.GameAudioController.Instance?.PlayPositive();
            GameSession.EnsureHumanCharacters();
            SceneManager.LoadScene("CharacterSelectScene");
        }

        private IEnumerator PulseTapPrompt(TextMeshProUGUI prompt)
        {
            if (prompt == null) yield break;
            Color baseColor = prompt.color;
            while (_tapToStartPanel != null && _tapToStartPanel.activeSelf)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 3.5f) + 1f) * 0.5f; // 0..1
                var c = baseColor;
                c.a = Mathf.Lerp(0.25f, 1.0f, t);
                prompt.color = c;
                yield return null;
            }
        }

        private void ResolveBgImage()
        {
            if (_bgImage != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var bg = canvas.transform.Find("TitleBackgroundArt");
                if (bg != null) _bgImage = bg.GetComponent<Image>();
            }
        }

        private void FadeBackgroundAlpha(float targetAlpha, float duration = 0.35f)
        {
            ResolveBgImage();
            if (_bgImage == null) return;
            if (_bgFadeRoutine != null) StopCoroutine(_bgFadeRoutine);
            _bgFadeRoutine = StartCoroutine(FadeBgAlphaRoutine(targetAlpha, duration));
        }

        private IEnumerator FadeBgAlphaRoutine(float targetAlpha, float duration)
        {
            if (_bgImage == null) yield break;
            float startAlpha = _bgImage.color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                var c = _bgImage.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                _bgImage.color = c;
                yield return null;
            }
            var finalColor = _bgImage.color;
            finalColor.a = targetAlpha;
            _bgImage.color = finalColor;
        }

        private void ShowPanel(GameObject target)
        {
            if (_tapToStartPanel   != null && target != null) _tapToStartPanel.SetActive(false);
            if (_titlePanel        != null) _titlePanel.SetActive(_titlePanel        == target);
            if (_settingsPanel     != null) _settingsPanel.SetActive(_settingsPanel  == target);
            if (_achievementsPanel != null) _achievementsPanel.SetActive(_achievementsPanel == target);

            if (target == _titlePanel)
            {
                // メニュー画面では背景pngの透明度を上げて（薄く透かして）UIを見やすくする
                FadeBackgroundAlpha(0.35f, 0.35f);
                var panelImg = _titlePanel?.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.enabled = true;
                    panelImg.color = new Color(0.08f, 0.10f, 0.16f, 0.55f);
                }
            }
            ApplyPanelFocus(target);
        }

        /// <summary>パネル切替時にキーボードフォーカスを適切な場所へ置く/返す（WCAG 2.4.3）。</summary>
        private void ApplyPanelFocus(GameObject target)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;

            if (target == _titlePanel)
            {
                var restore = _lastTitleSelection;
                _lastTitleSelection = null;
                var start = _titlePanel != null
                    ? _titlePanel.transform.Find("StartButton")?.GetComponent<Button>()
                    : null;
                var sel = restore != null && restore.activeInHierarchy
                    ? restore
                    : start != null && start.interactable ? start.gameObject : null;
                if (sel != null) es.SetSelectedGameObject(sel);
                return;
            }

            // モーダル側へ移る前に、開くきっかけとなったタイトル側の選択を記憶
            _lastTitleSelection = es.currentSelectedGameObject;

            Button first = null;
            if (target == _achievementsPanel)
            {
                first = _achievementsPanelController != null ? _achievementsPanelController.FirstTabButton : null;
                first ??= _achievementsPanel?.transform.Find("CloseButton")?.GetComponent<Button>();
            }
            else if (target == _settingsPanel)
            {
                var slider = _settingsPanel != null
                    ? _settingsPanel.transform.Find("HumanCountSlider")?.GetComponent<Slider>()
                    : null;
                if (slider != null && slider.interactable)
                {
                    es.SetSelectedGameObject(slider.gameObject);
                    return;
                }
                first = _settingsPanel?.transform.Find("CloseButton")?.GetComponent<Button>();
            }

            if (first != null && first.interactable && first.gameObject.activeInHierarchy)
                es.SetSelectedGameObject(first.gameObject);
        }

        private void ApplySafeLayout()
        {
            // GUI（Sceneビュー/Inspector）で微調整された各ボタンのTransformを尊重する
            EnsureDefaultLayoutIfUnset("StartButton", 110f);
            EnsureDefaultLayoutIfUnset("DailyChallengeButton", 48f);
            EnsureDefaultLayoutIfUnset("SettingsButton", -42f);
            EnsureDefaultLayoutIfUnset("AchievementsButton", -104f);
            EnsureDefaultLayoutIfUnset("HelpButton", -166f);

            UiSafeLayout.DisableDecorativeRaycasts(transform);
        }

        private void EnsureDefaultLayoutIfUnset(string name, float defaultY)
        {
            var button = _titlePanel?.transform.Find(name)?.GetComponent<RectTransform>();
            if (button == null) return;
            if (button.sizeDelta.x <= 1f || button.sizeDelta.y <= 1f)
            {
                button.anchorMin = button.anchorMax = new Vector2(0.5f, 0.5f);
                button.pivot = new Vector2(0.5f, 0.5f);
                button.anchoredPosition = new Vector2(0f, defaultY);
                button.sizeDelta = new Vector2(360f, 52f);
            }
        }

        private void ApplyScreenChrome()
        {
            var titlePanelImg = _titlePanel?.GetComponent<Image>();
            if (titlePanelImg != null)
            {
                // Title1 画像が背景にあるため、TitlePanelの全画面Imageを無効化（青み・暗転防止）
                titlePanelImg.enabled = false;
            }

            ApplyPanelChrome(_settingsPanel?.transform, new Color(0.12f, 0.14f, 0.20f, 0.98f),
                new Color(0.50f, 0.66f, 0.88f, 0.78f));
            ApplyPanelChrome(_achievementsPanel?.transform, new Color(0.12f, 0.14f, 0.20f, 0.98f),
                new Color(0.56f, 0.78f, 0.58f, 0.78f));

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.GetComponentInParent<Button>() != null) continue;
                bool inModal = (_settingsPanel != null && tmp.transform.IsChildOf(_settingsPanel.transform))
                    || (_achievementsPanel != null && tmp.transform.IsChildOf(_achievementsPanel.transform));
                var isTitle = tmp.fontSize >= 32f;
                if (inModal)
                {
                    GameUiChrome.ApplyReadable(tmp,
                        isTitle ? new Color(1f, 0.94f, 0.70f, 1f) : GameUiChrome.MutedText,
                        isTitle ? FontStyles.Bold : FontStyles.Normal);
                }
                else
                {
                    // 明るいイラスト背景（Title1）上での視認性を確保（濃紺〜スレート系）
                    GameUiChrome.ApplyReadable(tmp,
                        isTitle ? new Color(0.12f, 0.15f, 0.26f, 1f) : new Color(0.26f, 0.30f, 0.42f, 1f),
                        isTitle ? FontStyles.Bold : FontStyles.Normal);
                }
            }

            foreach (var slider in GetComponentsInChildren<Slider>(true))
                KenneyUiStyler.StyleSlider(slider);
        }

        private static void ApplyPanelChrome(Transform panel, Color surface, Color accent)
        {
            if (panel == null) return;
            GameUiChrome.ApplySurface(panel, surface);
            GameUiChrome.ApplyAccentRail(panel, accent, 6f);
        }

        private GameObject FindChild(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.gameObject : null;
        }

        private void WireButton(string path, UnityEngine.Events.UnityAction action)
        {
            var t = transform.Find(path);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private T FindChildComponent<T>(string path) where T : Component
        {
            var t = transform.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }

        private void SetText(string path, string text)
        {
            var t = transform.Find(path);
            if (t == null) return;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            tmp.text = text;
            ApplyJapaneseFont(tmp);
        }

        public static void ApplyJapaneseFont(TextMeshProUGUI tmp) =>
            JapaneseFontProvider.Apply(tmp);

        public static TMPro.TMP_FontAsset LoadJapaneseFont() =>
            JapaneseFontProvider.Get();
    }
}
