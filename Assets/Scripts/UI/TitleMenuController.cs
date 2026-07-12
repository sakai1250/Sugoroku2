using UnityEngine;
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
        private GameObject  _titlePanel;
        private GameObject  _settingsPanel;
        private GameObject  _achievementsPanel;
        private AchievementsPanelController _achievementsPanelController;
        private Slider      _humanSlider;
        private Slider      _cpuSlider;
        private Slider      _boardSlider;
        private Slider      _difficultySlider;
        private Slider      _bgmVolumeSlider;
        private Slider      _seVolumeSlider;
        private Slider      _professorSlider;
        private float _bgmVolume = 0.7f;
        private float _seVolume  = 1.0f;
        private ProfessorType _professor = ProfessorType.None;

        private int _humanCount = 1;
        private int _cpuCount   = 1;
        private int _boardCells = (int)BoardLengthOption.Standard;
        private GameDifficulty _difficulty = GameDifficulty.Normal;

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

            _titlePanel        = FindChild("TitlePanel");
            _settingsPanel     = FindChild("SettingsPanel");
            _achievementsPanel = FindChild("AchievementsPanel");
            if (_achievementsPanel != null)
            {
                _achievementsPanelController = _achievementsPanel.GetComponent<AchievementsPanelController>();
                if (_achievementsPanelController == null)
                    _achievementsPanelController = _achievementsPanel.AddComponent<AchievementsPanelController>();
            }

            EnsureSettingsControls();
            EnsureDailyChallengeButton();
            ApplySafeLayout();
            WireButtons();
            RefreshSettingsLabels();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) KenneyUiStyler.StyleCanvas(canvas);
            ApplyScreenChrome();
            ShowPanel(_titlePanel);
            GameAudioController.Instance?.PlayTitleBgm();
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
                _professorSlider = CreateCountSlider(panel, "ProfessorSlider", new Vector2(140f, -230f), 0, 3);
                CreateTMP(panel, "ProfessorText", $"指導教員: {_professor.DisplayName()}", 20, new Vector2(-120f, -230f));
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
            handleRt.sizeDelta = new Vector2(18f, 18f);

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
            if (_titlePanel.transform.Find("DailyChallengeButton") != null) return;

            var startRt = _titlePanel.transform.Find("StartButton")?.GetComponent<RectTransform>();
            var pos = new Vector2(0f, -200f);

            var go = new GameObject("DailyChallengeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_titlePanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = startRt != null ? startRt.sizeDelta : new Vector2(240f, 48f);
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

        private void WireButtons()
        {
            WireButton("TitlePanel/StartButton",          OnStartGame);
            WireButton("TitlePanel/DailyChallengeButton", OnStartDailyChallenge);
            WireButton("TitlePanel/SettingsButton",       () => ShowPanel(_settingsPanel));
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
        }

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
            SetText("SettingsPanel/ProfessorText", $"指導教員: {_professor.DisplayName()}");
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

        private void ShowPanel(GameObject target)
        {
            if (_titlePanel        != null) _titlePanel.SetActive(_titlePanel        == target);
            if (_settingsPanel     != null) _settingsPanel.SetActive(_settingsPanel  == target);
            if (_achievementsPanel != null) _achievementsPanel.SetActive(_achievementsPanel == target);
        }

        private void ApplySafeLayout()
        {
            LayoutTitleMenuButton("StartButton", 96f);
            LayoutTitleMenuButton("DailyChallengeButton", 28f);
            LayoutTitleMenuButton("SettingsButton", -40f);
            LayoutTitleMenuButton("AchievementsButton", -108f);

            LayoutModalPanel(_settingsPanel?.transform);
            LayoutModalPanel(_achievementsPanel?.transform);
            UiSafeLayout.DisableDecorativeRaycasts(transform);
        }

        private void LayoutTitleMenuButton(string name, float y)
        {
            var button = _titlePanel?.transform.Find(name)?.GetComponent<RectTransform>();
            if (button == null) return;
            button.anchorMin = button.anchorMax = new Vector2(0.5f, 0.5f);
            button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = new Vector2(0f, y);
            button.sizeDelta = new Vector2(360f, 56f);
        }

        private static void LayoutModalPanel(Transform panel)
        {
            if (panel is not RectTransform panelRt) return;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(1080f, 860f);
            UiSafeLayout.LayoutCloseButton(panel, panel.Find("CloseButton"));
        }

        private void ApplyScreenChrome()
        {
            ApplyPanelChrome(_titlePanel?.transform, new Color(0.10f, 0.12f, 0.18f, 0.96f),
                new Color(0.86f, 0.68f, 0.28f, 0.88f));
            ApplyPanelChrome(_settingsPanel?.transform, new Color(0.12f, 0.14f, 0.20f, 0.98f),
                new Color(0.50f, 0.66f, 0.88f, 0.78f));
            ApplyPanelChrome(_achievementsPanel?.transform, new Color(0.12f, 0.14f, 0.20f, 0.98f),
                new Color(0.56f, 0.78f, 0.58f, 0.78f));

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.GetComponentInParent<Button>() != null) continue;
                var isTitle = tmp.fontSize >= 32f;
                GameUiChrome.ApplyReadable(tmp,
                    isTitle ? new Color(1f, 0.94f, 0.70f, 1f) : GameUiChrome.MutedText,
                    isTitle ? FontStyles.Bold : FontStyles.Normal);
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
