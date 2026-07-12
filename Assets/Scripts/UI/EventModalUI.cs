using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sugoroku.Game;
using Sugoroku.Data;
using Sugoroku.Network;
using Sugoroku.Board;
using Sugoroku.Audio;

namespace Sugoroku.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class EventModalUI : MonoBehaviour
    {
        public static EventModalUI Instance { get; private set; }

        [SerializeField] private GameObject      _panel;
        [SerializeField] private TextMeshProUGUI  _titleText;
        [SerializeField] private TextMeshProUGUI  _tagsText;
        [SerializeField] private TextMeshProUGUI  _descriptionText;
        [SerializeField] private Transform        _choiceButtonParent;

        private EventMaster _currentEvent;
        private PlayerData  _currentPlayer;
        private readonly List<Button> _buttons = new();
        private bool _hasVisibleContent;
        private int _showGeneration;
        private Image _modalBackdrop;
        private Transform _overlayRoot;
        private TextMeshProUGUI _instructionText;
        private Image _rareGlowPanel;

        public static bool HasVisibleModal
        {
            get
            {
                var modal = Resolve();
                return modal != null && modal.IsVisible;
            }
        }

        private static EventModalUI Resolve() =>
            Instance ?? Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);

        private bool IsVisible =>
            _hasVisibleContent &&
            EnsureCanvasGroup().alpha > 0.01f;

        private void Awake()
        {
            Instance = this;
            AutoBind();
            EnsureModalShell();
            EnsureCanvasGroup();
            EnsureInitialized();
        }

        public void EnsureAwakeAndReady()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            AutoBind();
            EnsureModalShell();
            EnsureCanvasGroup();
        }

        public void EnsureInitialized()
        {
            AutoBind();
            EnsureModalShell();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void AutoBind()
        {
            _panel ??= gameObject;
            _titleText ??= FindDeep<TextMeshProUGUI>(transform, "ModalTitle");
            _tagsText ??= FindDeep<TextMeshProUGUI>(transform, "ModalTags");
            _descriptionText ??= FindDeep<TextMeshProUGUI>(transform, "ModalDescription");
            _choiceButtonParent ??= FindDeep<Transform>(transform, "ChoiceButtonParent");
        }

        private static T FindDeep<T>(Transform root, string name) where T : Component
        {
            if (root == null) return null;
            if (root.name == name)
                return root.GetComponent<T>();
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep<T>(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>OnEventTriggered 未接続時のフォールバック。</summary>
        public static void ShowEventDirect(EventMaster ev, PlayerData player)
        {
            var modal = Resolve();
            modal?.EnsureAwakeAndReady();
            modal?.ShowEvent(ev, player);
        }

        public void ShowEventFromManager(EventMaster ev, PlayerData player) =>
            ShowEvent(ev, player);

        public void EnsureVisibleLayer() => BringToFront();

        public static void ShowPreview(EventMaster ev)
        {
            var modal = Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);
            modal?.ShowPreviewOnly(ev);
        }

        public static void ShowSquarePreview(Sugoroku.Data.SquareType type, string title, string description)
        {
            var modal = Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);
            modal?.ShowSquarePreviewOnly(type, title, description);
        }

        private void ShowPreviewOnly(EventMaster ev)
        {
            EnsureAwakeAndReady();
            SetRareGlow(false);
            UiLayerManager.ApplyEventModalOpen();
            _hasVisibleContent = true;
            ShowModalVisuals();
            EventModalMassBackdrop.Apply(transform, ev, null);
            BringToFront();
            EventModalLayout.Apply(transform, ev);
            if (_titleText != null) _titleText.text = ev.Title;
            if (_tagsText != null)
                _tagsText.text = ev.Tags != null && ev.Tags.Length > 0
                    ? string.Join("  ", System.Array.ConvertAll(ev.Tags, t => $"[{t}]"))
                    : "";
            if (_descriptionText != null) _descriptionText.text = ev.Description;
            ClearChoiceButtons();
            AddCloseButton();
            ConfigureModalRaycasts();
        }

        private void ShowSquarePreviewOnly(Sugoroku.Data.SquareType type, string title, string description)
        {
            EnsureAwakeAndReady();
            SetRareGlow(false);
            UiLayerManager.ApplyEventModalOpen();
            _hasVisibleContent = true;
            ShowModalVisuals();
            BringToFront();
            if (_titleText != null) _titleText.text = title;
            if (_tagsText != null) _tagsText.text = $"[{type}]";
            if (_descriptionText != null) _descriptionText.text = description;
            ClearChoiceButtons();
            AddCloseButton();
            ConfigureModalRaycasts();
        }

        private void ClearChoiceButtons()
        {
            foreach (var b in _buttons)
                if (b != null) Destroy(b.gameObject);
            _buttons.Clear();
        }

        private void AddCloseButton()
        {
            if (_choiceButtonParent == null) return;
            var btn = CreateChoiceButton(
                new EventChoice { Label = "閉じる" },
                true,
                null);
            btn.onClick.AddListener(() => HideModal());
            _buttons.Add(btn);
        }

        private void EnsureRareGlowPanel()
        {
            if (_rareGlowPanel != null || _titleText == null) return;

            var go = new GameObject("RareGlowPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_titleText.transform.parent, false);
            go.transform.SetSiblingIndex(_titleText.transform.GetSiblingIndex());

            var rt = (RectTransform)go.transform;
            var titleRt = (RectTransform)_titleText.transform;
            rt.anchorMin = titleRt.anchorMin;
            rt.anchorMax = titleRt.anchorMax;
            rt.pivot = titleRt.pivot;
            rt.anchoredPosition = titleRt.anchoredPosition;
            rt.sizeDelta = titleRt.sizeDelta + new Vector2(24f, 12f);

            _rareGlowPanel = go.GetComponent<Image>();
            _rareGlowPanel.color = new Color(1f, 0.84f, 0.2f, 0.35f);
            _rareGlowPanel.raycastTarget = false;
            _rareGlowPanel.gameObject.SetActive(false);
        }

        private void SetRareGlow(bool active)
        {
            EnsureRareGlowPanel();
            if (_rareGlowPanel != null) _rareGlowPanel.gameObject.SetActive(active);
        }

        private void ShowEvent(EventMaster ev, PlayerData player)
        {
            if (ev == null || player == null) return;

            EnsureAwakeAndReady();
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            UiLayerManager.ApplyEventModalOpen();

            _currentEvent  = ev;
            _currentPlayer = player;
            _hasVisibleContent = true;
            _showGeneration++;
            int generation = _showGeneration;

            ShowModalVisuals();

            EventModalMassBackdrop.Apply(transform, ev, player);
            EventModalLayout.Apply(transform, ev);
            if (_titleText != null)
            {
                _titleText.text = ev.Title;
                JapaneseFontProvider.Apply(_titleText);
            }

            bool isRareEvent = ev.IsRare;
            SetRareGlow(isRareEvent);
            if (isRareEvent) GameAudioController.Instance?.PlayRareEventFanfare();

            if (_descriptionText != null)
            {
                _descriptionText.text = ev.Description;
                JapaneseFontProvider.Apply(_descriptionText);
            }
            if (_tagsText != null && ev.Tags != null && ev.Tags.Length > 0)
            {
                bool affinity = CharacterTagAffinity.HasAffinity(player.Character, ev.Tags);
                string tagsLine = string.Join("  ", System.Array.ConvertAll(ev.Tags, t => $"[{t}]"));
                _tagsText.text = affinity ? $"{tagsLine}  ◎相性" : tagsLine;
                JapaneseFontProvider.Apply(_tagsText);
            }

            EnsureModalInstruction(
                $"{PlayerIdentity.FormatHudLabel(player)} — 「{ev.Title}」— 下の選択肢を選んでください");

            foreach (var b in _buttons) if (b != null) Destroy(b.gameObject);
            _buttons.Clear();

            bool forceSingle = ev.ChoiceCount == 1;
            int selectable = 0;

            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                var choice = ev.GetChoice(i);
                if (choice == null) continue;

                bool canSelect = EventRobustnessValidator.CanSelectChoice(ev, choice, player);
                EventChoiceEvaluator.MeetsConditions(choice, player, forceSingle, out var failReason);
                if (canSelect) selectable++;

                int idx = i;
                var btn = CreateChoiceButton(choice, canSelect, forceSingle ? null : failReason, player);
                btn.interactable = canSelect;
                btn.onClick.AddListener(() => OnChoiceSelected(idx));
                _buttons.Add(btn);
            }

            if (selectable == 0)
            {
                if (EventRobustnessValidator.CanUseVirtueRescue(player))
                    AddVirtueRescueButton(player);
                if (!player.IsCpu)
                    AddStuckFallbackButton(ev, player);
                if (!player.IsCpu && _buttons.Count == 0)
                {
                    EnsureModalInstruction("選択できる肢がありません。積んだ徳が足りるか確認してください。");
                    AddForceSkipButton();
                }
            }

            if (_choiceButtonParent == null)
            {
                UnityEngine.Debug.LogError("EventModalUI: ChoiceButtonParent が見つかりません。");
                HideModal();
                return;
            }

            _choiceButtonParent.SetAsLastSibling();
            ConfigureModalRaycasts();
            ClearEventSystemSelection();
            BringToFront();
            EnsureMassBackdropLayer();
            EnsureWorldDimmer()?.ShowForEvent();

            if (player.IsCpu)
                StartCoroutine(CpuPick(player, ev, generation));
        }

        private static void ClearEventSystemSelection()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        private void AddVirtueRescueButton(PlayerData player)
        {
            if (player.Virtue < GameConfig.VirtueRescueThreshold) return;

            var rescueChoice = new EventChoice
            {
                Label        = "積んだ徳が効く（周囲がフォロー）",
                MoneyChange  = 0,
                IfScoreChange= 0,
                MentalChange = 10,
                VirtueChange = -5
            };
            var btn = CreateChoiceButton(rescueChoice, true, null);
            btn.onClick.AddListener(() =>
                EventManager.Instance.RunHumanChoiceResolution(_currentPlayer, rescueChoice, DismissModalUi));
            _buttons.Add(btn);
        }

        private System.Collections.IEnumerator CpuPick(PlayerData player, EventMaster ev, int generation)
        {
            yield return new WaitForSeconds(1.2f);
            if (generation != _showGeneration || !_hasVisibleContent) yield break;

            int idx = EventRobustnessValidator.FirstSelectableIndex(ev, player);
            if (idx >= 0)
            {
                OnChoiceSelected(idx);
                yield break;
            }

            if (player.Virtue >= GameConfig.VirtueRescueThreshold)
            {
                var rescueChoice = new EventChoice
                {
                    MentalChange = 10,
                    VirtueChange = -5
                };
                EventManager.Instance.RunHumanChoiceResolution(player, rescueChoice, DismissModalUi);
                yield break;
            }

            int fallback = EventRobustnessValidator.FirstUnconditionalIndex(ev);
            if (fallback >= 0)
            {
                UnityEngine.Debug.LogWarning($"CPU イベント {ev.EventId}: 条件付きのみのため代替肢 {fallback} を選択。");
                OnChoiceSelected(fallback);
                yield break;
            }

            UnityEngine.Debug.LogError($"イベント {ev.EventId}: 進行不能（§7.1 違反）。ターンをスキップします。");
            CloseAndEndTurn();
        }

        private void AddForceSkipButton()
        {
            var btn = CreateChoiceButton(
                new EventChoice { Label = "（進行不能のためスキップ）" },
                true,
                null);
            btn.onClick.AddListener(CloseAndEndTurn);
            _buttons.Add(btn);
        }

        private void AddStuckFallbackButton(EventMaster ev, PlayerData player)
        {
            int fallback = EventRobustnessValidator.FirstUnconditionalIndex(ev);
            if (fallback < 0) return;

            var choice = ev.GetChoice(fallback);
            var btn = CreateChoiceButton(
                new EventChoice { Label = $"（代替）{choice?.Label ?? "進む"}" },
                true,
                "他の選択肢は条件を満たしていません");
            int idx = fallback;
            btn.onClick.AddListener(() => OnChoiceSelected(idx));
            _buttons.Add(btn);
        }

        private void OnChoiceSelected(int index)
        {
            var choice = _currentEvent.GetChoice(index);
            if (choice == null) return;

            if (!EventRobustnessValidator.CanSelectChoice(_currentEvent, choice, _currentPlayer))
            {
                UnityEngine.Debug.LogWarning($"選択不可: {_currentEvent.EventId} choice {index}");
                return;
            }

            var net = NetworkSessionHost.Instance;
            if (net != null && net.IsOnline)
            {
                net.SubmitEventChoice(index, _currentEvent.EventId);
                HideModal();
                return;
            }

            EventManager.Instance.RunHumanChoiceResolution(_currentPlayer, choice, DismissModalUi);
        }

        private void DismissModalUi()
        {
            HideModal();
            EnsureWorldDimmer()?.Hide();
            Board.BoardCameraController.Instance?.RestoreFramedView();
            _showGeneration++;
        }

        private void CloseAndEndTurn()
        {
            _showGeneration++;
            HideModal();
            EnsureWorldDimmer()?.Hide();
            Board.BoardCameraController.Instance?.RestoreFramedView();
            TurnManager.Instance.EndTurn();
            FindFirstObjectByType<GameHUD>()?.RefreshAll();
        }

        private void HideModal()
        {
            _hasVisibleContent = false;
            HideImmediate();
            UiLayerManager.ApplyEventModalClosed();
        }

        private void HideImmediate()
        {
            EventModalMassBackdrop.Clear(transform);
            var cg = EnsureCanvasGroup();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            if (_modalBackdrop != null)
                _modalBackdrop.enabled = false;
            if (_instructionText != null)
                _instructionText.gameObject.SetActive(false);
        }

        private void ShowModalVisuals()
        {
            var cg = EnsureCanvasGroup();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
            if (_modalBackdrop != null)
            {
                _modalBackdrop.enabled = true;
                _modalBackdrop.color = new Color(0f, 0f, 0f, 0.42f);
                _modalBackdrop.transform.SetAsFirstSibling();
            }
        }

        private void EnsureModalShell()
        {
            _panel ??= gameObject;

            if (_modalBackdrop == null)
            {
                var backdropGo = transform.Find("ModalBackdrop");
                if (backdropGo == null)
                {
                    backdropGo = new GameObject("ModalBackdrop", typeof(RectTransform)).transform;
                    backdropGo.SetParent(transform, false);
                    backdropGo.SetAsFirstSibling();
                    var rt = backdropGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    _modalBackdrop = backdropGo.gameObject.AddComponent<Image>();
                    _modalBackdrop.color = new Color(0f, 0f, 0f, 0.55f);
                }
                else
                {
                    _modalBackdrop = backdropGo.GetComponent<Image>() ?? backdropGo.gameObject.AddComponent<Image>();
                }
                _modalBackdrop.raycastTarget = true;
            }

            if (_choiceButtonParent == null)
            {
                var existing = transform.Find("ChoiceButtonParent");
                if (existing == null)
                    existing = FindDeep<Transform>(transform, "ChoiceButtonParent");
                _choiceButtonParent = existing;
            }

            if (_choiceButtonParent == null)
            {
                var choiceGo = new GameObject("ChoiceButtonParent", typeof(RectTransform));
                choiceGo.transform.SetParent(transform, false);
                _choiceButtonParent = choiceGo.transform;
            }
        }

        private void EnsureMassBackdropLayer()
        {
            var backdrop = transform.Find(EventModalMassBackdrop.RootName);
            if (backdrop == null) return;

            var modalBackdrop = transform.Find("ModalBackdrop");
            int index = modalBackdrop != null ? modalBackdrop.GetSiblingIndex() + 1 : 0;
            backdrop.SetSiblingIndex(index);
        }

        private void BringToFront()
        {
            var root = EnsureOverlayRoot();
            if (root != null && transform.parent != root)
                transform.SetParent(root, false);

            StretchToParent(transform as RectTransform);
            root.SetAsLastSibling();
            transform.SetAsLastSibling();

            EnsureModalShell();
            ShowModalVisuals();
        }

        private Transform EnsureOverlayRoot()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetAsLastSibling();
                return _overlayRoot;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return transform.parent;

            _overlayRoot = UiLayerManager.EnsureEventModalRoot(canvas);
            return _overlayRoot;
        }

        private void EnsureModalInstruction(string message)
        {
            if (_instructionText == null)
            {
                var existing = transform.Find("ModalInstruction");
                if (existing == null)
                {
                    var go = new GameObject("ModalInstruction", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, 248f);
                    rt.sizeDelta = new Vector2(720f, 52f);
                    _instructionText = go.AddComponent<TextMeshProUGUI>();
                    _instructionText.alignment = TextAlignmentOptions.Center;
                    _instructionText.fontSize = EventModalLayout.PreviewFontSize + 2f;
                    _instructionText.color = new Color(1f, 0.96f, 0.78f);
                    _instructionText.raycastTarget = false;
                    JapaneseFontProvider.Apply(_instructionText);
                    HudTextStyle.ApplyOutlineSafe(_instructionText, 0.10f, new Color(0f, 0f, 0f, 0.80f));
                }
                else
                {
                    _instructionText = existing.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_instructionText != null)
            {
                _instructionText.text = message ?? "";
                _instructionText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private static void StretchToParent(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private CanvasGroup EnsureCanvasGroup()
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null)
                cg = gameObject.AddComponent<CanvasGroup>();
            cg.ignoreParentGroups = true;
            return cg;
        }

        private void ConfigureModalRaycasts()
        {
            var panelImg = _panel?.GetComponent<Image>();
            if (panelImg != null)
                panelImg.raycastTarget = false;

            if (_titleText != null) _titleText.raycastTarget = false;
            if (_tagsText != null) _tagsText.raycastTarget = false;
            if (_descriptionText != null) _descriptionText.raycastTarget = false;

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.GetComponentInParent<Button>() == null)
                    tmp.raycastTarget = false;
            }
        }

        private static GameWorldPresentationDimmer EnsureWorldDimmer()
        {
            var d = GameWorldPresentationDimmer.Instance;
            if (d != null) return d;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            return canvas.gameObject.AddComponent<GameWorldPresentationDimmer>();
        }

        private Button CreateChoiceButton(EventChoice c, bool interactable, string failReason, PlayerData player = null)
        {
            var go = new GameObject("ChoiceButton");
            go.transform.SetParent(_choiceButtonParent, false);
            var img = go.AddComponent<Image>();
            float alpha = interactable ? 0.95f : EventModalLayout.DisabledAlpha;
            img.color = new Color(0.18f, 0.2f, 0.32f, alpha);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.interactable = interactable;
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, EventModalLayout.DisabledAlpha);
            btn.colors = colors;
            GameUiChrome.ApplyChoiceButton(btn, interactable);

            var rt = go.GetComponent<RectTransform>();
            float height = interactable ? 100f : 124f;
            rt.sizeDelta = new Vector2(680f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight       = height;
            le.preferredWidth  = 680f;

            var column = new GameObject("Column", typeof(RectTransform));
            column.transform.SetParent(go.transform, false);
            var columnV = column.AddComponent<VerticalLayoutGroup>();
            columnV.padding = new RectOffset(12, 12, 8, 8);
            columnV.spacing = 4f;
            columnV.childAlignment = TextAnchor.UpperLeft;
            columnV.childControlWidth = columnV.childControlHeight = true;
            columnV.childForceExpandWidth = true;
            columnV.childForceExpandHeight = false;
            var columnRt = column.GetComponent<RectTransform>();
            columnRt.anchorMin = Vector2.zero;
            columnRt.anchorMax = Vector2.one;
            columnRt.offsetMin = columnRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(column.transform, false);
            var contentV = content.AddComponent<VerticalLayoutGroup>();
            contentV.spacing = 4f;
            contentV.childAlignment = TextAnchor.UpperLeft;
            contentV.childControlWidth = contentV.childControlHeight = true;
            contentV.childForceExpandWidth = true;
            var contentLe = content.AddComponent<LayoutElement>();
            contentLe.flexibleWidth = 1f;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(content.transform, false);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = c.Label;
            labelTmp.fontSize = EventModalLayout.ChoiceFontSize;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.65f);
            labelTmp.raycastTarget = false;
            JapaneseFontProvider.Apply(labelTmp);

            CreateChoiceEffectRow(content.transform, c, player, interactable);

            if (!string.IsNullOrEmpty(failReason))
            {
                var failGo = new GameObject("FailReason");
                failGo.transform.SetParent(column.transform, false);
                var failTmp = failGo.AddComponent<TextMeshProUGUI>();
                failTmp.text = EventChoicePreview.FormatFailBadge(failReason);
                failTmp.fontSize = EventModalLayout.PreviewFontSize;
                failTmp.alignment = TextAlignmentOptions.Left;
                failTmp.color = new Color(1f, 0.35f, 0.35f);
                failTmp.richText = true;
                failTmp.raycastTarget = false;
                JapaneseFontProvider.Apply(failTmp);
                var failLe = failGo.AddComponent<LayoutElement>();
                failLe.minHeight = 18f;
                failLe.preferredHeight = 22f;
            }

            return btn;
        }

        private static void CreateChoiceEffectRow(Transform parent, EventChoice choice, PlayerData player, bool interactable)
        {
            var rowGo = new GameObject("EffectPreviewRow", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            int money = choice.MoneyChange;
            int mental = choice.MentalChange;
            int ifScore = choice.IfScoreChange;
            int virtue = choice.VirtueChange;
            if (player != null)
            {
                money = PlayerStatRules.ClampMoney(player.Money + choice.MoneyChange) - player.Money;
                mental = PlayerStatRules.ClampMental(player.Mental + choice.MentalChange, player.MaxMental) - player.Mental;
                ifScore = PlayerStatRules.ClampIfScore(player.IfScore + choice.IfScoreChange) - player.IfScore;
                // Virtue is intentionally unbounded in PlayerData.ApplyStatChange, so the raw delta is exact.
            }

            bool any = false;
            any |= CreateEffectSlot(rowGo.transform, "所持金", money, "万", interactable);
            any |= CreateEffectSlot(rowGo.transform, "メンタル", mental, "", interactable);
            any |= CreateEffectSlot(rowGo.transform, "IF", ifScore, "", interactable);
            any |= CreateEffectSlot(rowGo.transform, "徳", virtue, "", interactable);

            if (!any)
                CreateNeutralSlot(rowGo.transform, interactable);
        }

        private static bool CreateEffectSlot(Transform parent, string label, int delta, string unit, bool interactable)
        {
            if (delta == 0) return false;
            var go = new GameObject($"{label}Effect", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            string sign = delta > 0 ? "+" : "";
            tmp.text = $"{label} {sign}{delta}{unit}";
            tmp.fontSize = EventModalLayout.PreviewFontSize;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = delta > 0
                ? new Color(0.40f, 1f, 0.58f, interactable ? 1f : 0.60f)
                : new Color(1f, 0.38f, 0.38f, interactable ? 1f : 0.60f);
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = label == "所持金" ? 142f : 116f;
            le.minHeight = 26f;
            return true;
        }

        private static void CreateNeutralSlot(Transform parent, bool interactable)
        {
            var go = new GameObject("NoEffect", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "変化なし";
            tmp.fontSize = EventModalLayout.PreviewFontSize;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = interactable ? GameUiChrome.MutedText : new Color(0.70f, 0.72f, 0.76f, 0.70f);
            tmp.raycastTarget = false;
            JapaneseFontProvider.Apply(tmp);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 140f;
            le.minHeight = 26f;
        }
    }
}
