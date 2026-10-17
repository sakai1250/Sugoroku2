using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Sugoroku.Game;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("上部リソースバー")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private TextMeshProUGUI _ifScoreText;
        [SerializeField] private TextMeshProUGUI _mentalText;
        [SerializeField] private TextMeshProUGUI _virtueText;
        [SerializeField] private Slider          _mentalSlider;

        [Header("補助HUD")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _turnStateText;
        [SerializeField] private TextMeshProUGUI _goalDistanceText;
        [SerializeField] private TextMeshProUGUI _tuitionDistanceText;
        [SerializeField] private TextMeshProUGUI _skipTurnsText;
        [SerializeField] private TextMeshProUGUI _ignoreEventsText;

        [Header("操作")]
        [SerializeField] private Image           _diceIconImage;
        [SerializeField] private TextMeshProUGUI _diceResultText;
        [SerializeField] private Button          _rollButton;
        [SerializeField] private Button          _skillButton;
        [SerializeField] private TextMeshProUGUI _skillButtonText;
        [SerializeField] private Button          _menuButton;
        private Button                               _speedButton;
        private TextMeshProUGUI                      _speedButtonText;
        private static float                         _currentGameSpeed = 1.0f;

        private Button _itemDiceRerollButton;
        private Button _itemMentalHealButton;
        private Button _itemMoneyBonusButton;
        private TextMeshProUGUI _itemDiceRerollText;
        private TextMeshProUGUI _itemMentalHealText;
        private TextMeshProUGUI _itemMoneyBonusText;
        private TextMeshProUGUI _playerRoleText;
        private TextMeshProUGUI _skillInfoTitle;
        private TextMeshProUGUI _skillInfoDesc;
        private TextMeshProUGUI _skillInfoState;
        private TextMeshProUGUI _statusSummaryText;

        [Header("ポーズ/詳細")]
        [SerializeField] private PauseMenuUI     _pauseMenu;
        [SerializeField] private TextMeshProUGUI _logText;

        /// <summary>モーダル閉鎖後のキーボードフォーカス復帰先（WCAG 2.4.3）。</summary>
        public Button RollButton => _rollButton;

        private readonly System.Collections.Generic.Queue<string> _logQueue = new();
        private const int MaxLogLines = 8;
        private bool _isSubscribed;
        private DiceHudAnimator _diceHudAnimator;
        private HudStatFlash _statFlash;
        private readonly Dictionary<TextMeshProUGUI, Coroutine> _statCountRoutines = new();
        private Image _portraitImage;
        private System.Action<PlayerData, Sugoroku.Visual.CharacterMood> _moodChangedHandler;
        private System.Action<PlayerData> _turnStartedHandler;
        private System.Action<PlayerData, string> _squareEffectHandler;

        private void Awake()
        {
            BindAllReferences();
            DisableDecorativeRaycasts();
            EnsureLayout();
        }

        private void Start()
        {
            BindAllReferences();
            DisableDecorativeRaycasts();
            EnsureLayout();
            if (!TrySubscribe()) StartCoroutine(SubscribeWhenReady());

            _rollButton?.onClick.AddListener(OnRollButtonClicked);
            _skillButton?.onClick.AddListener(OnSkillButtonClicked);
            _menuButton?.onClick.AddListener(OnMenuButtonClicked);
            SetupSpeedToggleButton();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                KenneyUiStyler.StyleCanvas(canvas);
                _diceIconImage ??= KenneyUiStyler.EnsureDiceDisplay(canvas.transform);
            }

            _diceIconImage ??= UiBindingUtility.FindComponent<Image>("DiceIcon");
            SetupDiceHudAnimator();
            SetupStatFlash();
            KenneyUiStyler.ApplyDiceIconSprite(_diceIconImage, 6);
            ApplyReadableStyles();
            RefreshAll();
        }

        private void ApplyReadableStyles()
        {
            if (_playerNameText != null)
            {
                HudTextStyle.ApplyReadable(_playerNameText, HudTextStyle.Scale(21f), new Color(1f, 0.95f, 0.7f), true);
                _playerNameText.textWrappingMode = TextWrappingModes.NoWrap;
                _playerNameText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (_turnStateText != null)
            {
                HudTextStyle.ApplyReadable(_turnStateText, HudTextStyle.Scale(22f),
                    new Color(0.55f, 1f, 0.75f), true);
                _turnStateText.textWrappingMode = TextWrappingModes.NoWrap;
                _turnStateText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (_goalDistanceText != null)
                HudTextStyle.ApplyInfo(_goalDistanceText, new Color(0.88f, 0.92f, 1f));
            if (_tuitionDistanceText != null)
                HudTextStyle.ApplyInfo(_tuitionDistanceText, new Color(0.88f, 0.92f, 1f));
            if (_skipTurnsText != null)
                HudTextStyle.ApplyInfo(_skipTurnsText, new Color(1f, 0.75f, 0.5f));
            if (_ignoreEventsText != null)
                HudTextStyle.ApplyInfo(_ignoreEventsText, new Color(0.7f, 0.85f, 1f));
            if (_diceResultText != null)
                HudTextStyle.ApplyReadable(_diceResultText, HudTextStyle.Scale(16f), Color.white, true);
            if (_logText != null)
            {
                HudTextStyle.ApplyLog(_logText);
                _logText.alignment = TextAlignmentOptions.BottomLeft;
            }
            if (_skillButtonText != null)
                HudTextStyle.ApplyReadable(_skillButtonText, HudTextStyle.Scale(17f), Color.white, true);
        }

        private void SetupStatFlash()
        {
            _statFlash = GetComponent<HudStatFlash>();
            if (_statFlash == null) _statFlash = gameObject.AddComponent<HudStatFlash>();
            _statFlash.Bind(_moneyText, _ifScoreText, _mentalText, _virtueText, _mentalSlider);

            if (GetComponent<StatJuicePresenter>() == null)
                gameObject.AddComponent<StatJuicePresenter>();
        }

        private void EnsureLayout()
        {
            if (StaticUiAuthoring.IsBaked(transform)) return;
            var layout = GetComponentInParent<GameMainLayout>();
            if (layout == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    layout = canvas.GetComponent<GameMainLayout>();
                    if (layout == null) layout = canvas.gameObject.AddComponent<GameMainLayout>();
                }
            }
            layout?.ApplyLayout();
        }

        private void BindAllReferences()
        {
            _moneyText  ??= FindBar("MoneyText");
            _ifScoreText ??= FindBar("IfScoreText");
            _mentalText ??= FindBar("MentalText");
            _virtueText ??= FindBar("VirtueText");
            _mentalSlider ??= UiBindingUtility.FindComponent<Slider>("MentalSlider");

            _playerNameText ??= FindAny("PlayerNameText");
            _turnStateText  ??= FindAny("HudText");
            _goalDistanceText ??= FindAny("GoalDistanceText");
            _tuitionDistanceText ??= FindAny("TuitionDistanceText");
            _skipTurnsText ??= FindAny("SkipTurnsText");
            _ignoreEventsText ??= FindAny("IgnoreEventsText");

            _rollButton  ??= UiBindingUtility.FindComponent<Button>("RollButton");
            _skillButton ??= UiBindingUtility.FindComponent<Button>("SkillButton");
            _menuButton  ??= UiBindingUtility.FindComponent<Button>("MenuButton");
            _diceIconImage  ??= UiBindingUtility.FindComponent<Image>("DiceIcon");
            _diceResultText ??= UiBindingUtility.FindComponent<TextMeshProUGUI>("DiceResult");
            _logText ??= UiBindingUtility.FindComponent<TextMeshProUGUI>("LogText");
            _pauseMenu ??= Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);

            if (_skillButtonText == null && _skillButton != null)
                _skillButtonText = _skillButton.GetComponentInChildren<TextMeshProUGUI>(true);

            EnsureLeftPanelDetails();
            EnsurePortraitDisplay();
        }

        private void EnsureLeftPanelDetails()
        {
            var panel = transform.parent?.Find("PlayerInfoPanel") ?? transform.Find("PlayerInfoPanel");
            if (panel == null) return;

            // 専攻・特性テキスト（名前の下）
            if (_playerRoleText == null)
            {
                var existingRole = panel.Find("PlayerRoleText");
                if (existingRole != null)
                {
                    _playerRoleText = existingRole.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var go = new GameObject("PlayerRoleText", typeof(RectTransform));
                    go.transform.SetParent(panel, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(90f, -54f);
                    rt.sizeDelta = new Vector2(236f, 26f);
                    _playerRoleText = go.AddComponent<TextMeshProUGUI>();
                    _playerRoleText.fontSize = 13f;
                    _playerRoleText.color = new Color(0.65f, 0.85f, 1f);
                    _playerRoleText.alignment = TextAlignmentOptions.Left;
                    JapaneseFontProvider.Apply(_playerRoleText);
                }
            }

            // スキル情報カード
            if (_skillInfoTitle == null)
            {
                var card = panel.Find("SkillInfoCard");
                if (card == null)
                {
                    var cardGo = new GameObject("SkillInfoCard", typeof(RectTransform), typeof(Image));
                    cardGo.transform.SetParent(panel, false);
                    var cRt = cardGo.GetComponent<RectTransform>();
                    cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 1f);
                    cRt.pivot = new Vector2(0f, 1f);
                    cRt.anchoredPosition = new Vector2(14f, -272f);
                    cRt.sizeDelta = new Vector2(314f, 82f);

                    var bg = cardGo.GetComponent<Image>();
                    bg.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
                    bg.type = Image.Type.Sliced;
                    bg.color = new Color(0.12f, 0.15f, 0.22f, 0.92f);
                    bg.raycastTarget = false;
                    GameUiChrome.ApplyAccentRail(cardGo.transform, new Color(0.86f, 0.72f, 0.32f, 0.85f), 3f);

                    _skillInfoTitle = CreateCardText(cardGo.transform, "SkillTitle", new Vector2(10f, -8f), new Vector2(294f, 20f), 13f, new Color(1f, 0.90f, 0.45f), FontStyles.Bold);
                    _skillInfoDesc  = CreateCardText(cardGo.transform, "SkillDesc",  new Vector2(10f, -28f), new Vector2(294f, 30f), 11f, new Color(0.88f, 0.92f, 0.98f));
                    _skillInfoState = CreateCardText(cardGo.transform, "SkillState", new Vector2(10f, -58f), new Vector2(294f, 18f), 11f, new Color(0.55f, 1f, 0.75f));
                }
                else
                {
                    _skillInfoTitle = card.Find("SkillTitle")?.GetComponent<TextMeshProUGUI>();
                    _skillInfoDesc  = card.Find("SkillDesc")?.GetComponent<TextMeshProUGUI>();
                    _skillInfoState = card.Find("SkillState")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // インベントリカード（所持アイテム枠）
            var invCard = panel.Find("InventoryCard");
            if (invCard == null)
            {
                var invGo = new GameObject("InventoryCard", typeof(RectTransform), typeof(Image));
                invGo.transform.SetParent(panel, false);
                var iRt = invGo.GetComponent<RectTransform>();
                iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 1f);
                iRt.pivot = new Vector2(0f, 1f);
                iRt.anchoredPosition = new Vector2(14f, -366f);
                iRt.sizeDelta = new Vector2(314f, 222f);

                var bg = invGo.GetComponent<Image>();
                bg.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.12f, 0.15f, 0.22f, 0.92f);
                bg.raycastTarget = false;
                GameUiChrome.ApplyAccentRail(invGo.transform, new Color(0.35f, 0.75f, 0.95f, 0.85f), 3f);

                CreateCardText(invGo.transform, "InvHeader", new Vector2(10f, -8f), new Vector2(294f, 20f), 13f, new Color(0.55f, 0.90f, 1f), FontStyles.Bold, "🎒 所持アイテム");

                invCard = invGo.transform;
            }

            if (_itemDiceRerollButton == null)
            {
                (_itemDiceRerollButton, _itemDiceRerollText) =
                    CreateItemSlot(invCard, "ItemButton_DiceReroll", -32f, "もう一振り券", "使う", () =>
                    {
                        var p = GameManager.Instance?.GetCurrentPlayer();
                        if (p != null && !p.IsCpu) GameManager.Instance.UseDiceRerollItem(p);
                        RefreshAll();
                    });
            }

            if (_itemMentalHealButton == null)
            {
                (_itemMentalHealButton, _itemMentalHealText) =
                    CreateItemSlot(invCard, "ItemButton_MentalHeal", -94f, "気分転換ドリンク", "飲む", () =>
                    {
                        var p = GameManager.Instance?.GetCurrentPlayer();
                        if (p != null && !p.IsCpu) GameManager.Instance.UseMentalHealItem(p);
                        RefreshAll();
                    });
            }

            if (_itemMoneyBonusButton == null)
            {
                (_itemMoneyBonusButton, _itemMoneyBonusText) =
                    CreateItemSlot(invCard, "ItemButton_MoneyBonus", -156f, "臨時収入券", "使う", () =>
                    {
                        var p = GameManager.Instance?.GetCurrentPlayer();
                        if (p != null && !p.IsCpu) GameManager.Instance.UseMoneyBonusItem(p);
                        RefreshAll();
                    });
            }

            // ステータスサマリーカード
            if (_statusSummaryText == null)
            {
                var sumCard = panel.Find("StatusSummaryCard");
                if (sumCard == null)
                {
                    var sumGo = new GameObject("StatusSummaryCard", typeof(RectTransform), typeof(Image));
                    sumGo.transform.SetParent(panel, false);
                    var sRt = sumGo.GetComponent<RectTransform>();
                    sRt.anchorMin = sRt.anchorMax = new Vector2(0f, 1f);
                    sRt.pivot = new Vector2(0f, 1f);
                    sRt.anchoredPosition = new Vector2(14f, -596f);
                    sRt.sizeDelta = new Vector2(314f, 52f);

                    var bg = sumGo.GetComponent<Image>();
                    bg.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
                    bg.type = Image.Type.Sliced;
                    bg.color = new Color(0.10f, 0.13f, 0.18f, 0.88f);
                    bg.raycastTarget = false;

                    _statusSummaryText = CreateCardText(sumGo.transform, "SummaryText", new Vector2(8f, -6f), new Vector2(298f, 40f), 12f, new Color(0.85f, 0.90f, 0.98f));
                }
                else
                {
                    _statusSummaryText = sumCard.Find("SummaryText")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private TextMeshProUGUI CreateCardText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style = FontStyles.Normal, string defaultText = "")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.text = defaultText;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            JapaneseFontProvider.Apply(tmp);
            return tmp;
        }

        private (Button, TextMeshProUGUI) CreateItemSlot(Transform parent, string buttonName, float yPos, string defaultName, string btnLabel, System.Action onClick)
        {
            // The editor-baked slot is a scene object. Reuse it instead of creating
            // a duplicate during each play session.
            var bakedSlot = parent.Find($"Slot_{buttonName}");
            if (bakedSlot != null)
            {
                var bakedButton = bakedSlot.Find(buttonName)?.GetComponent<Button>();
                var bakedText = bakedSlot.Find("ItemInfoText")?.GetComponent<TextMeshProUGUI>();
                if (bakedButton != null)
                {
                    bakedButton.onClick.RemoveAllListeners();
                    bakedButton.onClick.AddListener(() => onClick?.Invoke());
                    return (bakedButton, bakedText);
                }
            }
            var slotGo = new GameObject($"Slot_{buttonName}", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(parent, false);
            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 1f);
            slotRt.pivot = new Vector2(0f, 1f);
            slotRt.anchoredPosition = new Vector2(8f, yPos);
            slotRt.sizeDelta = new Vector2(298f, 58f);

            var slotBg = slotGo.GetComponent<Image>();
            slotBg.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
            slotBg.type = Image.Type.Sliced;
            slotBg.color = new Color(0.16f, 0.20f, 0.28f, 0.90f);
            slotBg.raycastTarget = false;

            var textGo = new GameObject("ItemInfoText", typeof(RectTransform));
            textGo.transform.SetParent(slotGo.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(8f, 0f);
            tRt.sizeDelta = new Vector2(206f, 50f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 12f;
            tmp.text = defaultName;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            JapaneseFontProvider.Apply(tmp);

            var btnGo = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(slotGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-6f, 0f);
            btnRt.sizeDelta = new Vector2(74f, 36f);

            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.text = btnLabel;
            lblTmp.fontSize = 14f;
            lblTmp.alignment = TextAlignmentOptions.Center;
            var lRt = lblTmp.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            JapaneseFontProvider.Apply(lblTmp);

            var btn = btnGo.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());

            return (btn, tmp);
        }

        private void DisableDecorativeRaycasts()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage != null) rootImage.raycastTarget = false;

            if (_diceIconImage != null) _diceIconImage.raycastTarget = false;
            if (_diceResultText != null) _diceResultText.raycastTarget = false;
        }

        private TextMeshProUGUI FindBar(string n)
        {
            var bar = transform.parent?.Find("ResourceBar") ?? transform.Find("ResourceBar");
            if (bar != null)
            {
                var direct = bar.Find(n)?.GetComponent<TextMeshProUGUI>();
                if (direct != null) return direct;
                foreach (Transform child in bar)
                {
                    var nested = child.Find(n)?.GetComponent<TextMeshProUGUI>();
                    if (nested != null) return nested;
                }
            }
            return UiBindingUtility.FindComponent<TextMeshProUGUI>(n);
        }

        private TextMeshProUGUI FindAny(string n) =>
            transform.Find(n)?.GetComponent<TextMeshProUGUI>() ??
            UiBindingUtility.FindComponent<TextMeshProUGUI>(n);

        private IEnumerator SubscribeWhenReady()
        {
            while (!TrySubscribe()) yield return null;
            OnStateChanged(TurnManager.Instance.CurrentState);
            RefreshAll();
        }

        private bool TrySubscribe()
        {
            if (_isSubscribed) return true;
            if (TurnManager.Instance == null || DiceRoller.Instance == null ||
                GameManager.Instance == null || !GameManager.Instance.IsInitialized)
                return false;

            _turnStartedHandler ??= _ => RefreshAll();
            _squareEffectHandler ??= (_, msg) => AppendLog(msg);

            TurnManager.Instance.OnStateChanged += OnStateChanged;
            TurnManager.Instance.OnTurnStarted  += _turnStartedHandler;
            DiceRoller.Instance.OnRollComplete  += OnDiceResult;
            GameManager.Instance.OnLog          += AppendLog;
            GameManager.Instance.OnSquareEffect += _squareEffectHandler;

            foreach (var modal in Object.FindObjectsByType<EventModalUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                modal.EnsureInitialized();
            }

            SetupDiceHudAnimator();
            if (DiceRoller.Instance != null)
                DiceRoller.Instance.RegisterHudAnimator(_diceHudAnimator);

            _moodChangedHandler ??= (player, mood) =>
            {
                var cur = GameManager.Instance?.GetCurrentPlayer();
                if (cur != null && cur.Index == player.Index)
                    RefreshPortrait(player);
            };
            CharacterMoodManager.OnMoodChanged += _moodChangedHandler;

            _isSubscribed = true;
            return true;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1.0f;
            if (!_isSubscribed) return;
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnStateChanged -= OnStateChanged;
                if (_turnStartedHandler != null)
                    TurnManager.Instance.OnTurnStarted -= _turnStartedHandler;
            }
            if (DiceRoller.Instance != null)
                DiceRoller.Instance.OnRollComplete -= OnDiceResult;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLog -= AppendLog;
                if (_squareEffectHandler != null)
                    GameManager.Instance.OnSquareEffect -= _squareEffectHandler;
            }
            if (_moodChangedHandler != null)
            {
                CharacterMoodManager.OnMoodChanged -= _moodChangedHandler;
            }
        }

        private void OnStateChanged(TurnState state)
        {
            RefreshActionButtons(state);

            if (_turnStateText != null)
            {
                _turnStateText.text = FormatStateLabel(state);
                HudTextStyle.ApplyReadable(_turnStateText,
                    HudTextStyle.Scale(22f),
                    GetStateColor(state), true);
                _turnStateText.textWrappingMode = TextWrappingModes.NoWrap;
                _turnStateText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static string FormatStateLabel(TurnState state) => state switch
        {
            TurnState.TurnStart  => "★ ターン開始!",
            TurnState.WaitAction => "★ ダイス待ち!",
            TurnState.Moving     => ">> 移動中!",
            TurnState.MassCheck  => "◇ マス確認!",
            TurnState.Event      => "★ イベント!",
            TurnState.Apply      => "OK 効果発動!",
            TurnState.TurnEnd    => "♪ ターン終了!",
            _ => ""
        };

        private static Color GetStateColor(TurnState state) => state switch
        {
            TurnState.WaitAction => new Color(1f, 0.92f, 0.42f, 1f),
            TurnState.Moving     => new Color(0.50f, 0.94f, 1f, 1f),
            TurnState.MassCheck  => new Color(0.72f, 1f, 0.62f, 1f),
            TurnState.Event      => new Color(1f, 0.66f, 0.88f, 1f),
            TurnState.Apply      => new Color(0.66f, 1f, 0.70f, 1f),
            TurnState.TurnEnd    => new Color(0.76f, 0.82f, 1f, 1f),
            _                    => new Color(1f, 0.95f, 0.70f, 1f)
        };

        private void RefreshActionButtons(TurnState? stateOverride = null)
        {
            var player = GameManager.Instance?.GetCurrentPlayer();
            var state = stateOverride ?? TurnManager.Instance?.CurrentState;
            bool rolling = DiceRoller.Instance != null && DiceRoller.Instance.IsRolling;
            bool canAct = state == TurnState.WaitAction && player != null && !player.IsCpu && !rolling;
            if (_rollButton  != null) _rollButton.interactable  = canAct;
            if (_skillButton != null) _skillButton.interactable = canAct && !player.SkillUsedThisTurn;
            if (_itemDiceRerollButton != null) _itemDiceRerollButton.interactable = canAct && player.ItemDiceRerollCount > 0;
            if (_itemMentalHealButton != null) _itemMentalHealButton.interactable = canAct && player.ItemMentalHealCount > 0;
            if (_itemMoneyBonusButton != null) _itemMoneyBonusButton.interactable = canAct && player.ItemMoneyBonusCount > 0;
        }

        private void SetupDiceHudAnimator()
        {
            if (_diceIconImage == null) return;

            _diceHudAnimator = _diceIconImage.GetComponent<DiceHudAnimator>();
            if (_diceHudAnimator == null)
                _diceHudAnimator = _diceIconImage.gameObject.AddComponent<DiceHudAnimator>();

            _diceHudAnimator.Bind(_diceIconImage, _diceResultText);

            if (DiceRoller.Instance != null)
                DiceRoller.Instance.RegisterHudAnimator(_diceHudAnimator);
        }

        private void OnDiceResult(int value)
        {
            if (_diceResultText != null) _diceResultText.text = $"サイコロ: {value}";
        }

        private void OnRollButtonClicked()
        {
            var p = GameManager.Instance?.GetCurrentPlayer();
            if (p == null || p.IsCpu || DiceRoller.Instance == null) return;
            if (!DiceRoller.Instance.CanRoll())
            {
                RefreshAll();
                return;
            }
            DiceRoller.Instance.Roll(requireLocalHuman: true);
        }

        private void OnSkillButtonClicked()
        {
            var p = GameManager.Instance?.GetCurrentPlayer();
            if (p == null || p.IsCpu || p.SkillUsedThisTurn) return;
            GameManager.Instance.UsePlayerSkill(p);
        }

        private void OnMenuButtonClicked() => _pauseMenu?.Open();

        public void RefreshAll()
        {
            var player = GameManager.Instance?.GetCurrentPlayer();
            if (player == null) return;
            RefreshActionButtons();

            if (_moneyText  != null) _moneyText.text  = ResourceHudVisuals.FormatMoney(player.Money);
            if (_ifScoreText != null) _ifScoreText.text = ResourceHudVisuals.FormatIf(player.IfScore);
            if (_mentalText != null) _mentalText.text = ResourceHudVisuals.FormatMental(player.Mental, player.MaxMental);
            if (_virtueText != null) _virtueText.text = ResourceHudVisuals.FormatVirtue(player.Virtue);

            if (_mentalSlider != null)
            {
                _mentalSlider.minValue = 0f;
                _mentalSlider.maxValue = Mathf.Max(1, player.MaxMental);
                _mentalSlider.value    = Mathf.Clamp(player.Mental, 0, player.MaxMental);
                UpdateMentalSliderFill(player);
            }

            var canvas = GetComponentInParent<Canvas>();
            ResourceHudVisuals.Apply(player, canvas != null ? canvas.transform : transform);

            if (_playerNameText != null)
            {
                _playerNameText.text = PlayerIdentity.FormatHudLabel(player);
                HudTextStyle.ApplyReadable(_playerNameText, HudTextStyle.Scale(22f), player.PieceTint, true);
            }

            int board = Board.BoardManager.Instance?.BoardSize ?? GameConfig.BoardSize;
            int remain = board - 1 - player.BoardPosition;
            if (_goalDistanceText != null) _goalDistanceText.text = $"ゴールまで {remain}マス";
            int tuition = Board.BoardManager.Instance?.GetNextTuitionIndex(player) ?? 0;
            if (_tuitionDistanceText != null)
            {
                bool hasTuitionAhead = Board.BoardManager.Instance != null &&
                    Board.BoardManager.Instance.HasTuitionAhead(player.BoardPosition, player.ActiveBranch);
                _tuitionDistanceText.text = hasTuitionAhead
                    ? $"学費まで あと{tuition}マス"
                    : "学費マスはもうありません";
            }
            if (_skipTurnsText != null) _skipTurnsText.text = player.SkipTurns > 0 ? $"休み×{player.SkipTurns}" : "";
            if (_playerRoleText != null)
                _playerRoleText.text = $"専攻: {player.Character.DisplayName()}";

            if (_skillButtonText != null) _skillButtonText.text = $"ワザ: {player.Character.SkillName()}";

            if (_skillInfoTitle != null)
                _skillInfoTitle.text = $"★ 固有ワザ: {player.Character.SkillName()}";
            if (_skillInfoDesc != null)
                _skillInfoDesc.text = player.Character.SkillDescription();
            if (_skillInfoState != null)
            {
                bool used = player.SkillUsedThisTurn;
                _skillInfoState.text = used ? "<color=#888888>【使用済み】</color>" : "<color=#55ff88>【発動可能】（右下アクション）</color>";
            }

            if (_itemDiceRerollText != null)
                _itemDiceRerollText.text = $"<b>🎲 もう一振り券</b>  <color=#ffd700>×{player.ItemDiceRerollCount}</color>\n<size=11><color=#b0c4de>ダイスをもう1回振れる</color></size>";
            if (_itemMentalHealText != null)
                _itemMentalHealText.text = $"<b>☕ 気分転換ドリンク</b>  <color=#ffd700>×{player.ItemMentalHealCount}</color>\n<size=11><color=#b0c4de>メンタル +{GameConfig.ItemMentalHealAmount} 回復</color></size>";
            if (_itemMoneyBonusText != null)
                _itemMoneyBonusText.text = $"<b>💰 臨時収入券</b>  <color=#ffd700>×{player.ItemMoneyBonusCount}</color>\n<size=11><color=#b0c4de>資金 +{GameConfig.ItemMoneyBonusAmount} 万円獲得</color></size>";

            if (_statusSummaryText != null)
                _statusSummaryText.text = $"資金 {player.Money}万 / IF {player.IfScore}pt / 徳 {player.Virtue}pt";

            RefreshPortrait(player);
            EnsureAllPlayersStrip()?.Refresh();
        }

        /// <summary>全員の状況を常時見られるミニ一覧（無ければHUDキャンバスに生成）。</summary>
        private AllPlayersStatusStrip EnsureAllPlayersStrip()
        {
            var existing = FindFirstObjectByType<AllPlayersStatusStrip>(FindObjectsInactive.Exclude);
            if (existing != null) return existing;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            return canvas.gameObject.AddComponent<AllPlayersStatusStrip>();
        }

        public void AnimateStatChange(PlayerData player, int money, int ifScore, int mental, int virtue)
        {
            if (player == null) return;
            float duration = GameConfig.AnimationDuration(GameConfig.FloatingTextDuration);
            if (money != 0)
                AnimateCounter(_moneyText, player.Money - money, player.Money, duration, ResourceHudVisuals.FormatMoney);
            if (ifScore != 0)
                AnimateCounter(_ifScoreText, player.IfScore - ifScore, player.IfScore, duration, ResourceHudVisuals.FormatIf);
            if (mental != 0)
                AnimateCounter(_mentalText, player.Mental - mental, player.Mental, duration,
                    v => ResourceHudVisuals.FormatMental(v, player.MaxMental));
            if (virtue != 0)
                AnimateCounter(_virtueText, player.Virtue - virtue, player.Virtue, duration, ResourceHudVisuals.FormatVirtue);
        }

        private void AnimateCounter(TextMeshProUGUI tmp, int from, int to, float duration, System.Func<int, string> formatter)
        {
            if (tmp == null || formatter == null) return;
            if (_statCountRoutines.TryGetValue(tmp, out var existing) && existing != null)
                StopCoroutine(existing);
            _statCountRoutines[tmp] = StartCoroutine(AnimateCounterCoroutine(tmp, from, to, duration, formatter));
        }

        private IEnumerator AnimateCounterCoroutine(TextMeshProUGUI tmp, int from, int to, float duration, System.Func<int, string> formatter)
        {
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, JuiceMath.EaseOutQuad(t)));
                tmp.text = formatter(value);
                yield return null;
            }
            tmp.text = formatter(to);
            _statCountRoutines.Remove(tmp);
        }

        private void UpdateMentalSliderFill(PlayerData player)
        {
            if (_mentalSlider?.fillRect == null) return;
            var fill = _mentalSlider.fillRect.GetComponent<Image>();
            if (fill == null) return;
            fill.color = ResourceHudVisuals.GetMentalIconColor(player);
        }

        private void AppendLog(string msg)
        {
            _logQueue.Enqueue(msg);
            while (_logQueue.Count > MaxLogLines) _logQueue.Dequeue();
            if (_logText != null)
            {
                _logText.text = string.Join("\n", _logQueue);
                HudTextStyle.ApplyLog(_logText);
            }
        }

        private void EnsurePortraitDisplay()
        {
            if (_portraitImage != null) return;
            var panel = transform.parent?.Find("PlayerInfoPanel") ?? transform.Find("PlayerInfoPanel");
            if (panel == null) return;

            var container = panel.Find("PlayerPortraitContainer");
            if (container == null)
            {
                var go = new GameObject("PlayerPortraitContainer", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(panel, false);
                var cRt = go.GetComponent<RectTransform>();
                cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 1f);
                cRt.pivot     = new Vector2(0f, 1f);
                cRt.anchoredPosition = new Vector2(14f, -14f);
                cRt.sizeDelta = new Vector2(68f, 68f);

                var bg = go.GetComponent<Image>();
                bg.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
                bg.raycastTarget = false;

                var portGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portGo.transform.SetParent(go.transform, false);
                var pRt = portGo.GetComponent<RectTransform>();
                pRt.anchorMin = Vector2.zero;
                pRt.anchorMax = Vector2.one;
                pRt.offsetMin = new Vector2(3f, 3f);
                pRt.offsetMax = new Vector2(-3f, -3f);

                _portraitImage = portGo.GetComponent<Image>();
                _portraitImage.preserveAspect = true;
                _portraitImage.raycastTarget = false;
            }
            else
            {
                _portraitImage = container.Find("Portrait")?.GetComponent<Image>();
            }
        }

        private void RefreshPortrait(PlayerData player)
        {
            EnsurePortraitDisplay();
            if (_portraitImage == null || player == null) return;

            var mood = CharacterMoodManager.GetMood(player);
            var sprite = Sugoroku.Visual.CharacterVisualAssets.GetSprite(player.Character, mood);
            if (sprite != null)
                _portraitImage.sprite = sprite;
        }

        private void SetupSpeedToggleButton()
        {
            if (_speedButton != null) return;
            var parent = _menuButton != null ? _menuButton.transform.parent : transform;
            if (parent == null) return;

            var existing = parent.Find("SpeedToggleButton");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("SpeedToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                if (_menuButton != null)
                {
                    var mRt = _menuButton.GetComponent<RectTransform>();
                    rt.anchorMin = mRt.anchorMin;
                    rt.anchorMax = mRt.anchorMax;
                    rt.pivot = mRt.pivot;
                    // メニューボタンの左隣に配置
                    rt.anchoredPosition = new Vector2(mRt.anchoredPosition.x - mRt.sizeDelta.x - 12f, mRt.anchoredPosition.y);
                    rt.sizeDelta = new Vector2(mRt.sizeDelta.x, mRt.sizeDelta.y);
                }
                else
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-130f, -20f);
                    rt.sizeDelta = new Vector2(80f, 36f);
                }

                var img = go.GetComponent<Image>();
                img.sprite = Board.BoardVisualUtility.GetPixelCardSprite();
                img.type = Image.Type.Sliced;
                img.color = new Color(0.18f, 0.28f, 0.45f, 0.95f);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform, false);
                var tRt = textGo.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;

                _speedButtonText = textGo.GetComponent<TextMeshProUGUI>();
                _speedButtonText.fontSize = 14f;
                _speedButtonText.fontStyle = FontStyles.Bold;
                _speedButtonText.alignment = TextAlignmentOptions.Center;
                _speedButtonText.color = Color.white;
                JapaneseFontProvider.Apply(_speedButtonText);
            }

            _speedButton = go.GetComponent<Button>();
            _speedButtonText ??= go.GetComponentInChildren<TextMeshProUGUI>();
            UpdateSpeedButtonVisual();

            _speedButton.onClick.RemoveAllListeners();
            _speedButton.onClick.AddListener(ToggleSpeed);
        }

        private void ToggleSpeed()
        {
            if (_currentGameSpeed <= 1.0f)
            {
                _currentGameSpeed = 2.0f;
            }
            else
            {
                _currentGameSpeed = 1.0f;
            }
            Time.timeScale = _currentGameSpeed;
            UpdateSpeedButtonVisual();
            Sugoroku.Audio.GameAudioController.Instance?.PlayConfirm();
        }

        private void UpdateSpeedButtonVisual()
        {
            if (_speedButtonText != null)
            {
                _speedButtonText.text = _currentGameSpeed > 1.0f ? "⏩ 2x" : "▶ 1x";
            }
            var img = _speedButton?.GetComponent<Image>();
            if (img != null)
            {
                img.color = _currentGameSpeed > 1.0f 
                    ? new Color(0.9f, 0.45f, 0.1f, 0.95f) 
                    : new Color(0.18f, 0.28f, 0.45f, 0.95f);
            }
        }
    }
}
