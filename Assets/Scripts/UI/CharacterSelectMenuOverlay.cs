using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.Board;
using Sugoroku.Audio;

namespace Sugoroku.UI
{
    /// <summary>
    /// キャラ選択画面（統合画面）の右上ふちに配置されたメニューから開く
    /// 設定モーダルおよび実績・図鑑モーダル。
    /// </summary>
    public class CharacterSelectMenuOverlay : MonoBehaviour
    {
        private GameObject _settingsOverlay;
        private GameObject _achievementsOverlay;

        // 設定用コントロール
        private Slider _humanSlider;
        private Slider _cpuSlider;
        private Slider _boardSlider;
        private Slider _difficultySlider;
        private Slider _professorSlider;
        private Slider _bgmSlider;
        private Slider _seSlider;
        private Slider _textSizeSlider;

        private TextMeshProUGUI _humanText;
        private TextMeshProUGUI _cpuText;
        private TextMeshProUGUI _boardText;
        private TextMeshProUGUI _difficultyText;
        private TextMeshProUGUI _professorText;
        private TextMeshProUGUI _bgmText;
        private TextMeshProUGUI _seText;
        private TextMeshProUGUI _textSizeText;
        private TextMeshProUGUI _reduceMotionText;

        // 実績用コントロール
        private TextMeshProUGUI _achBodyText;
        private Button _achTabBtn;
        private Button _galleryTabBtn;
        private bool _isAchievementsTab = true;

        private System.Action _onSettingsChangedCallback;

        public void Bind(System.Action onSettingsChanged)
        {
            _onSettingsChangedCallback = onSettingsChanged;
        }

        // ==========================================
        //  設定モーダル
        // ==========================================
        public void OpenSettings()
        {
            if (_settingsOverlay == null)
                BuildSettingsModal();

            _settingsOverlay.SetActive(true);
            RefreshSettingsValues();
        }

        public void CloseSettings()
        {
            if (_settingsOverlay != null)
                _settingsOverlay.SetActive(false);

            GameSession.SaveSettings();
            _onSettingsChangedCallback?.Invoke();
        }

        private void BuildSettingsModal()
        {
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;

            // 背景ディマー
            _settingsOverlay = new GameObject("SettingsModalOverlay", typeof(RectTransform), typeof(Image));
            _settingsOverlay.transform.SetParent(parent, false);
            var oRt = _settingsOverlay.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = oRt.offsetMax = Vector2.zero;

            var dimImg = _settingsOverlay.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.70f);
            dimImg.raycastTarget = true;

            // ウィンドウ
            var win = new GameObject("Window", typeof(RectTransform), typeof(Image));
            win.transform.SetParent(_settingsOverlay.transform, false);
            var wRt = win.GetComponent<RectTransform>();
            wRt.anchorMin = wRt.anchorMax = new Vector2(0.5f, 0.5f);
            wRt.pivot = new Vector2(0.5f, 0.5f);
            wRt.sizeDelta = new Vector2(820f, 660f);

            var winImg = win.GetComponent<Image>();
            winImg.sprite = BoardVisualUtility.GetPixelCardSprite();
            winImg.type = Image.Type.Sliced;
            winImg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);
            GameUiChrome.ApplyAccentRail(win.transform, new Color(0.86f, 0.68f, 0.28f, 0.95f), 4f);

            // タイトル
            var title = CreateTMP(win.transform, "Title", "ゲーム設定", 24f, new Vector2(0f, 290f), new Vector2(400f, 40f), FontStyles.Bold);
            title.color = new Color(1f, 0.92f, 0.65f);
            title.alignment = TextAlignmentOptions.Center;

            // 閉じるボタン（右上 ✕）
            var closeX = CreateButton(win.transform, "CloseX", "✕", new Vector2(365f, 290f), new Vector2(44f, 40f), CloseSettings);
            GameUiChrome.ApplyButton(closeX, primary: false);

            // 設定項目コンテナ（2列構成）
            // 左列: 人数・コース長・難易度・指導教員 (x: -190f)
            float leftX = -190f;
            float rightX = 190f;
            float rowY = 220f;
            float rowGap = 72f;

            // 人間プレイヤー数
            _humanText = CreateTMP(win.transform, "HumanText", "人間プレイヤー: 1", 15f, new Vector2(leftX, rowY + 18f), new Vector2(340f, 24f));
            _humanSlider = CreateSlider(win.transform, "HumanSlider", new Vector2(leftX, rowY - 10f), 1, 4, v =>
            {
                GameSession.HumanCount = (int)v;
                ClampPlayerCounts();
                RefreshSettingsValues();
            });

            // CPU数
            rowY -= rowGap;
            _cpuText = CreateTMP(win.transform, "CpuText", "CPUプレイヤー: 1", 15f, new Vector2(leftX, rowY + 18f), new Vector2(340f, 24f));
            _cpuSlider = CreateSlider(win.transform, "CpuSlider", new Vector2(leftX, rowY - 10f), 0, 3, v =>
            {
                GameSession.CpuCount = (int)v;
                ClampPlayerCounts();
                RefreshSettingsValues();
            });

            // コース長
            rowY -= rowGap;
            _boardText = CreateTMP(win.transform, "BoardText", "コース: 標準 (20マス)", 15f, new Vector2(leftX, rowY + 18f), new Vector2(340f, 24f));
            _boardSlider = CreateSlider(win.transform, "BoardSlider", new Vector2(leftX, rowY - 10f), 0, 1, v =>
            {
                GameSession.BoardCellCount = v == 1 ? (int)BoardLengthOption.ExtraLong : (int)BoardLengthOption.Standard;
                BoardLayoutGenerator.Invalidate();
                RefreshSettingsValues();
            });

            // 難易度
            rowY -= rowGap;
            _difficultyText = CreateTMP(win.transform, "DifficultyText", "難易度: 普通", 15f, new Vector2(leftX, rowY + 18f), new Vector2(340f, 24f));
            _difficultySlider = CreateSlider(win.transform, "DifficultySlider", new Vector2(leftX, rowY - 10f), 0, 2, v =>
            {
                GameSession.Difficulty = (GameDifficulty)(int)v;
                RefreshSettingsValues();
            });

            // 指導教員
            rowY -= rowGap;
            _professorText = CreateTMP(win.transform, "ProfessorText", "指導教員: なし", 14f, new Vector2(leftX, rowY + 22f), new Vector2(340f, 36f));
            _professorSlider = CreateSlider(win.transform, "ProfessorSlider", new Vector2(leftX, rowY - 14f), 0, 3, v =>
            {
                GameSession.SelectedProfessor = (ProfessorType)(int)v;
                RefreshSettingsValues();
            });

            // 右列: BGM音量・SE音量・文字サイズ・動きを減らす
            rowY = 220f;

            // BGM音量
            _bgmText = CreateTMP(win.transform, "BgmText", "BGM音量: 70", 15f, new Vector2(rightX, rowY + 18f), new Vector2(340f, 24f));
            _bgmSlider = CreateSlider(win.transform, "BgmSlider", new Vector2(rightX, rowY - 10f), 0f, 1f, v =>
            {
                GameSession.BgmVolume = v;
                GameAudioController.Instance?.SetBgmVolume(v);
                RefreshSettingsValues();
            });

            // SE音量
            rowY -= rowGap;
            _seText = CreateTMP(win.transform, "SeText", "SE音量: 100", 15f, new Vector2(rightX, rowY + 18f), new Vector2(340f, 24f));
            _seSlider = CreateSlider(win.transform, "SeSlider", new Vector2(rightX, rowY - 10f), 0f, 1f, v =>
            {
                GameSession.SeVolume = v;
                GameAudioController.Instance?.SetSeVolume(v);
                RefreshSettingsValues();
            });

            // 文字サイズ
            rowY -= rowGap;
            _textSizeText = CreateTMP(win.transform, "TextSizeText", "文字サイズ: 標準", 15f, new Vector2(rightX, rowY + 18f), new Vector2(340f, 24f));
            _textSizeSlider = CreateSlider(win.transform, "TextSizeSlider", new Vector2(rightX, rowY - 10f), 0, 2, v =>
            {
                GameSession.UiTextScale = v switch { 0 => 0.85f, 2 => 1.25f, _ => 1f };
                RefreshSettingsValues();
            });

            // 動きを減らす
            rowY -= rowGap;
            var reduceBtn = CreateButton(win.transform, "ReduceMotionButton", "動きを減らす: OFF", new Vector2(rightX, rowY), new Vector2(340f, 44f), () =>
            {
                GameSession.ReduceMotion = !GameSession.ReduceMotion;
                RefreshSettingsValues();
            });
            _reduceMotionText = reduceBtn.GetComponentInChildren<TextMeshProUGUI>();
            GameUiChrome.ApplyButton(reduceBtn, primary: false);

            // 下部「完了」ボタン
            var doneBtn = CreateButton(win.transform, "DoneButton", "設定を完了する", new Vector2(0f, -280f), new Vector2(260f, 50f), CloseSettings);
            GameUiChrome.ApplyButton(doneBtn, primary: true);
        }

        private void ClampPlayerCounts()
        {
            int h = Mathf.Clamp(GameSession.HumanCount, 1, GameConfig.MaxPlayers);
            int c = Mathf.Clamp(GameSession.CpuCount, 0, GameConfig.MaxPlayers - 1);
            if (h + c > GameConfig.MaxPlayers)
                c = GameConfig.MaxPlayers - h;
            GameSession.HumanCount = h;
            GameSession.CpuCount = c;
            GameSession.EnsureHumanCharacters();
        }

        private void RefreshSettingsValues()
        {
            if (_humanSlider != null) _humanSlider.SetValueWithoutNotify(GameSession.HumanCount);
            if (_cpuSlider != null)   _cpuSlider.SetValueWithoutNotify(GameSession.CpuCount);
            if (_boardSlider != null) _boardSlider.SetValueWithoutNotify(GameSession.BoardCellCount == (int)BoardLengthOption.ExtraLong ? 1 : 0);
            if (_difficultySlider != null) _difficultySlider.SetValueWithoutNotify((int)GameSession.Difficulty);
            if (_professorSlider != null) _professorSlider.SetValueWithoutNotify((int)GameSession.SelectedProfessor);
            if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(GameSession.BgmVolume);
            if (_seSlider != null)  _seSlider.SetValueWithoutNotify(GameSession.SeVolume);
            if (_textSizeSlider != null)
            {
                float sc = GameSession.UiTextScale;
                _textSizeSlider.SetValueWithoutNotify(sc <= 0.95f ? 0 : (sc >= 1.15f ? 2 : 1));
            }

            if (_humanText != null) _humanText.text = $"人間プレイヤー: {GameSession.HumanCount}";
            if (_cpuText != null)   _cpuText.text   = $"CPUプレイヤー: {GameSession.CpuCount}（合計 {GameSession.HumanCount + GameSession.CpuCount}人）";
            if (_boardText != null) _boardText.text = $"コース: {DifficultyRules.GetBoardLengthLabel(GameSession.BoardCellCount)}";
            if (_difficultyText != null) _difficultyText.text = $"難易度: {DifficultyRules.GetLabel(GameSession.Difficulty)}";
            if (_professorText != null)
            {
                var prof = GameSession.SelectedProfessor;
                string summary = prof switch
                {
                    ProfessorType.Laissez       => "（IF 10%減 / メンタル被害15%減）",
                    ProfessorType.DrillSergeant => "（IF 15%増 / メンタル被害15%増）",
                    ProfessorType.Virtuous      => "（徳 20%増）",
                    _                           => "（効果なし）"
                };
                _professorText.text = $"指導教員: {prof.DisplayName()} {summary}";
            }
            if (_bgmText != null) _bgmText.text = $"BGM音量: {Mathf.RoundToInt(GameSession.BgmVolume * 100f)}";
            if (_seText != null)  _seText.text  = $"SE音量: {Mathf.RoundToInt(GameSession.SeVolume * 100f)}";
            if (_textSizeText != null)
            {
                float sc = GameSession.UiTextScale;
                string lbl = sc <= 0.95f ? "小" : (sc >= 1.15f ? "大" : "標準");
                _textSizeText.text = $"文字サイズ: {lbl}";
            }
            if (_reduceMotionText != null)
                _reduceMotionText.text = $"動きを減らす: {(GameSession.ReduceMotion ? "ON" : "OFF")}";
        }

        // ==========================================
        //  実績・進路図鑑モーダル
        // ==========================================
        public void OpenAchievements()
        {
            if (_achievementsOverlay == null)
                BuildAchievementsModal();

            _achievementsOverlay.SetActive(true);
            RenderAchievementsTab();
        }

        public void CloseAchievements()
        {
            if (_achievementsOverlay != null)
                _achievementsOverlay.SetActive(false);
        }

        private void BuildAchievementsModal()
        {
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;

            _achievementsOverlay = new GameObject("AchievementsModalOverlay", typeof(RectTransform), typeof(Image));
            _achievementsOverlay.transform.SetParent(parent, false);
            var oRt = _achievementsOverlay.GetComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = oRt.offsetMax = Vector2.zero;

            var dimImg = _achievementsOverlay.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.70f);
            dimImg.raycastTarget = true;

            var win = new GameObject("Window", typeof(RectTransform), typeof(Image));
            win.transform.SetParent(_achievementsOverlay.transform, false);
            var wRt = win.GetComponent<RectTransform>();
            wRt.anchorMin = wRt.anchorMax = new Vector2(0.5f, 0.5f);
            wRt.pivot = new Vector2(0.5f, 0.5f);
            wRt.sizeDelta = new Vector2(840f, 660f);

            var winImg = win.GetComponent<Image>();
            winImg.sprite = BoardVisualUtility.GetPixelCardSprite();
            winImg.type = Image.Type.Sliced;
            winImg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);
            GameUiChrome.ApplyAccentRail(win.transform, new Color(0.86f, 0.68f, 0.28f, 0.95f), 4f);

            var title = CreateTMP(win.transform, "Title", "実績・進路図鑑", 24f, new Vector2(0f, 290f), new Vector2(400f, 40f), FontStyles.Bold);
            title.color = new Color(1f, 0.92f, 0.65f);
            title.alignment = TextAlignmentOptions.Center;

            var closeX = CreateButton(win.transform, "CloseX", "✕", new Vector2(375f, 290f), new Vector2(44f, 40f), CloseAchievements);
            GameUiChrome.ApplyButton(closeX, primary: false);

            // タブボタン
            _achTabBtn = CreateButton(win.transform, "AchTabBtn", "実績一覧", new Vector2(-110f, 230f), new Vector2(180f, 44f), () =>
            {
                _isAchievementsTab = true;
                RenderAchievementsTab();
            });
            _galleryTabBtn = CreateButton(win.transform, "GalleryTabBtn", "進路図鑑", new Vector2(110f, 230f), new Vector2(180f, 44f), () =>
            {
                _isAchievementsTab = false;
                RenderAchievementsTab();
            });

            // スクロールコンテナ
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(win.transform, false);
            var sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(28f, 28f);
            sRt.offsetMax = new Vector2(-28f, -140f);

            var sImg = scrollGo.GetComponent<Image>();
            sImg.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);
            sImg.raycastTarget = true;

            var bodyGo = new GameObject("BodyText", typeof(RectTransform));
            bodyGo.transform.SetParent(scrollGo.transform, false);
            var bRt = bodyGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.offsetMin = new Vector2(16f, 0f);
            bRt.offsetMax = new Vector2(-16f, 0f);

            _achBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            _achBodyText.fontSize = 15f;
            _achBodyText.color = new Color(0.92f, 0.94f, 0.98f);
            _achBodyText.lineSpacing = 6f;
            _achBodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            JapaneseFontProvider.Apply(_achBodyText);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = bRt;
            scroll.viewport = sRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
        }

        private void RenderAchievementsTab()
        {
            if (_achBodyText == null) return;
            if (_isAchievementsTab)
            {
                var sb = new StringBuilder();
                int count = 0;
                int total = AchievementCatalog.All.Length;
                foreach (var def in AchievementCatalog.All)
                {
                    bool unlocked = AchievementStore.IsUnlocked(def.Id);
                    if (unlocked) count++;
                    string mark = unlocked ? "<color=#55ff88>✓ 解除</color>" : "<color=#888888>・未達成</color>";
                    string title = unlocked ? $"<b>{def.Title}</b>" : $"？？？ ({def.Description})";
                    sb.AppendLine($"{mark}  {title}");
                }
                sb.Insert(0, $"<color=#ffd700>達成状況: {count} / {total}</color>\n\n");
                _achBodyText.text = sb.ToString().TrimEnd();
            }
            else
            {
                var all = CareerOutcomeCatalog.EnumerateAll().ToList();
                int seenCount = all.Count(e => AchievementStore.IsCareerOutcomeSeen(e.Character, e.Rank));
                var sb = new StringBuilder();
                sb.AppendLine($"<color=#ffd700>閲覧済み進路: {seenCount} / {all.Count}</color>\n");
                foreach (var entry in all)
                {
                    bool seen = AchievementStore.IsCareerOutcomeSeen(entry.Character, entry.Rank);
                    string label = $"{entry.Character.DisplayName()} × ランク{entry.Rank}";
                    string line = seen
                        ? $"<color=#55ff88>✓</color>  <b>{label}</b>: {entry.CareerPath} — <size=12><color=#b0c4de>{entry.Subtitle}</color></size>"
                        : $"<color=#888888>・  {label}: ？？？</color>";
                    sb.AppendLine(line);
                }
                _achBodyText.text = sb.ToString().TrimEnd();
            }

            GameUiChrome.ApplyButton(_achTabBtn, primary: _isAchievementsTab);
            GameUiChrome.ApplyButton(_galleryTabBtn, primary: !_isAchievementsTab);
        }

        // ==========================================
        //  UI 生成ヘルパー
        // ==========================================
        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Vector2 pos, Vector2 size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            JapaneseFontProvider.Apply(tmp);
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.Center;
            var lRt = tmp.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            JapaneseFontProvider.Apply(tmp);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 pos, float min, float max, System.Action<float> onValueChanged)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(340f, 26f);

            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;

            // 背景バー
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.24f, 0.32f, 0.95f);

            // 塗りつぶし
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fRt = fillArea.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0f, 0.25f);
            fRt.anchorMax = new Vector2(1f, 0.75f);
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = new Color(0.35f, 0.75f, 0.95f, 0.95f);
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            // つまみ
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var hRt = handleArea.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = hRt.offsetMax = Vector2.zero;

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleArea.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(24f, 24f);
            var handleImg = handleGo.GetComponent<Image>();
            handleImg.color = new Color(1f, 0.92f, 0.65f, 1f);
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            slider.onValueChanged.AddListener(v => onValueChanged?.Invoke(v));
            return slider;
        }
    }
}

