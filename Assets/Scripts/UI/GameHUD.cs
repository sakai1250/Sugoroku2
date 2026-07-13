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

        private Button _itemDiceRerollButton;
        private Button _itemMentalHealButton;
        private Button _itemMoneyBonusButton;
        private TextMeshProUGUI _itemDiceRerollText;
        private TextMeshProUGUI _itemMentalHealText;
        private TextMeshProUGUI _itemMoneyBonusText;

        [Header("ポーズ/詳細")]
        [SerializeField] private PauseMenuUI     _pauseMenu;
        [SerializeField] private TextMeshProUGUI _logText;

        private readonly System.Collections.Generic.Queue<string> _logQueue = new();
        private const int MaxLogLines = 5;
        private bool _isSubscribed;
        private DiceHudAnimator _diceHudAnimator;
        private HudStatFlash _statFlash;
        private readonly Dictionary<TextMeshProUGUI, Coroutine> _statCountRoutines = new();
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
                HudTextStyle.ApplyReadable(_playerNameText, HudTextStyle.PlayerNameSize, new Color(1f, 0.95f, 0.7f), true);
            if (_turnStateText != null)
                HudTextStyle.ApplyReadable(_turnStateText, HudTextStyle.JuiceStatusFontSize,
                    new Color(0.55f, 1f, 0.75f), true);
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

            EnsureItemButtons();
        }

        private void EnsureItemButtons()
        {
            if (_itemDiceRerollButton == null)
            {
                (_itemDiceRerollButton, _itemDiceRerollText) =
                    FindOrCreateItemButton("ItemButton_DiceReroll", new Vector2(220f, -260f));
                _itemDiceRerollButton.onClick.RemoveAllListeners();
                _itemDiceRerollButton.onClick.AddListener(() =>
                {
                    var p = GameManager.Instance?.GetCurrentPlayer();
                    if (p != null && !p.IsCpu) GameManager.Instance.UseDiceRerollItem(p);
                    RefreshAll();
                });
            }

            if (_itemMentalHealButton == null)
            {
                (_itemMentalHealButton, _itemMentalHealText) =
                    FindOrCreateItemButton("ItemButton_MentalHeal", new Vector2(220f, -320f));
                _itemMentalHealButton.onClick.RemoveAllListeners();
                _itemMentalHealButton.onClick.AddListener(() =>
                {
                    var p = GameManager.Instance?.GetCurrentPlayer();
                    if (p != null && !p.IsCpu) GameManager.Instance.UseMentalHealItem(p);
                    RefreshAll();
                });
            }

            if (_itemMoneyBonusButton == null)
            {
                (_itemMoneyBonusButton, _itemMoneyBonusText) =
                    FindOrCreateItemButton("ItemButton_MoneyBonus", new Vector2(220f, -380f));
                _itemMoneyBonusButton.onClick.RemoveAllListeners();
                _itemMoneyBonusButton.onClick.AddListener(() =>
                {
                    var p = GameManager.Instance?.GetCurrentPlayer();
                    if (p != null && !p.IsCpu) GameManager.Instance.UseMoneyBonusItem(p);
                    RefreshAll();
                });
            }
        }

        private (Button, TextMeshProUGUI) FindOrCreateItemButton(string name, Vector2 pos)
        {
            var existingGo = UiBindingUtility.FindObject(name);
            if (existingGo != null)
            {
                var existingBtn = existingGo.GetComponent<Button>() ?? existingGo.AddComponent<Button>();
                var existingText = existingGo.GetComponentInChildren<TextMeshProUGUI>(true);
                return (existingBtn, existingText);
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280f, 55f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            JapaneseFontProvider.Apply(tmp);

            var btn = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btn, primary: false);
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

            _isSubscribed = true;
            return true;
        }

        private void OnDestroy()
        {
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
        }

        private void OnStateChanged(TurnState state)
        {
            RefreshActionButtons(state);

            if (_turnStateText != null)
            {
                _turnStateText.text = FormatStateLabel(state);
                HudTextStyle.ApplyReadable(_turnStateText,
                    HudTextStyle.JuiceStatusFontSize,
                    GetStateColor(state), true);
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
                HudTextStyle.ApplyReadable(_playerNameText, HudTextStyle.PlayerNameSize, player.PieceTint, true);
            }

            int board = Board.BoardManager.Instance?.BoardSize ?? GameConfig.BoardSize;
            int remain = board - 1 - player.BoardPosition;
            if (_goalDistanceText != null) _goalDistanceText.text = $"ゴールまで {remain}マス";
            int tuition = Board.BoardManager.Instance?.GetNextTuitionIndex(player) ?? 0;
            if (_tuitionDistanceText != null) _tuitionDistanceText.text = $"学費△{tuition}";
            if (_skipTurnsText != null) _skipTurnsText.text = player.SkipTurns > 0 ? $"休み×{player.SkipTurns}" : "";
            if (_ignoreEventsText != null) _ignoreEventsText.text = player.IgnoreNextEvents > 0 ? $"回避{player.IgnoreNextEvents}" : "";
            if (_skillButtonText != null) _skillButtonText.text = $"ワザ: {player.Character.SkillName()}";
            if (_itemDiceRerollText != null) _itemDiceRerollText.text = $"もう一振り券 ×{player.ItemDiceRerollCount}";
            if (_itemMentalHealText != null) _itemMentalHealText.text = $"気分転換ドリンク ×{player.ItemMentalHealCount}";
            if (_itemMoneyBonusText != null) _itemMoneyBonusText.text = $"臨時収入 ×{player.ItemMoneyBonusCount}";
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
    }
}
