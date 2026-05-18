using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sugoroku.Game;
using Sugoroku.Data;
using Sugoroku.Network;

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
        private bool _subscribed;
        private EventMaster _lastShownEvent;
        private PlayerData _lastShownPlayer;
        private int _lastShownFrame = -1;
        private bool _hasVisibleContent;

        public static bool HasVisibleModal => Instance != null && Instance.IsVisible;

        private bool IsVisible =>
            _hasVisibleContent &&
            _panel != null &&
            _panel.activeInHierarchy;

        private void Awake()
        {
            Instance = this;
            AutoBind();
            EnsureCanvasGroup();
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            AutoBind();
            if (_subscribed) return;
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered += ShowEvent;
                _subscribed = true;
            }
            else if (isActiveAndEnabled)
                StartCoroutine(SubscribeWhenReady());
        }

        private void Start()
        {
            EnsureInitialized();
            if (_panel != null && !_hasVisibleContent)
                _panel.SetActive(false);
        }

        private System.Collections.IEnumerator SubscribeWhenReady()
        {
            while (EventManager.Instance == null) yield return null;
            if (_subscribed) yield break;
            EventManager.Instance.OnEventTriggered += ShowEvent;
            _subscribed = true;
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
            if (EventManager.Instance != null && _subscribed)
                EventManager.Instance.OnEventTriggered -= ShowEvent;
        }

        /// <summary>OnEventTriggered 未接続時のフォールバック。</summary>
        public static void ShowEventDirect(EventMaster ev, PlayerData player)
        {
            var modal = Instance ?? Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);
            modal?.ShowEvent(ev, player);
        }

        public void ShowEventFromManager(EventMaster ev, PlayerData player) => ShowEvent(ev, player);

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
            _hasVisibleContent = true;
            if (_panel != null) _panel.SetActive(true);
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
            _hasVisibleContent = true;
            if (_panel != null) _panel.SetActive(true);
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
            btn.onClick.AddListener(() =>
            {
                if (_panel != null) _panel.SetActive(false);
                _hasVisibleContent = false;
            });
            _buttons.Add(btn);
        }

        private void ShowEvent(EventMaster ev, PlayerData player)
        {
            if (_lastShownFrame == Time.frameCount && _lastShownEvent == ev && _lastShownPlayer == player)
                return;
            _lastShownFrame = Time.frameCount;
            _lastShownEvent = ev;
            _lastShownPlayer = player;

            _currentEvent  = ev;
            _currentPlayer = player;
            _hasVisibleContent = true;
            if (_panel != null) _panel.SetActive(true);
            BringToFront();
            EnsureWorldDimmer()?.ShowForEvent();
            EventModalLayout.Apply(transform, ev);
            if (_titleText != null)
            {
                _titleText.text = ev.Title;
                JapaneseFontProvider.Apply(_titleText);
            }
            if (_descriptionText != null)
            {
                _descriptionText.text = ev.Description;
                JapaneseFontProvider.Apply(_descriptionText);
            }
            if (_tagsText != null && ev.Tags != null && ev.Tags.Length > 0)
            {
                _tagsText.text = string.Join("  ", System.Array.ConvertAll(ev.Tags, t => $"[{t}]"));
                JapaneseFontProvider.Apply(_tagsText);
            }

            GameStatusBanner.Show(
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
                    GameStatusBanner.Show("選択できる肢がありません。積んだ徳が足りるか確認してください。");
                    AddForceSkipButton();
                }
            }

            _choiceButtonParent?.SetAsLastSibling();
            ConfigureModalRaycasts();

            if (player.IsCpu)
                StartCoroutine(CpuPick(player, ev));
            else if (forceSingle && selectable == 1)
                StartCoroutine(AutoPickSingleChoice(0.6f));
        }

        private void AddVirtueRescueButton(PlayerData player)
        {
            if (player.Virtue < GameConfig.VirtueRescueThreshold) return;

            var btn = CreateChoiceButton(
                new EventChoice
                {
                    Label        = "積んだ徳が効く（周囲がフォロー）",
                    MoneyChange  = 0,
                    IfScoreChange= 0,
                    MentalChange = 10,
                    VirtueChange = -5
                },
                true,
                null);
            btn.onClick.AddListener(() =>
            {
                player.ApplyStatChange(0, 0, 10, -5);
                CloseAndEndTurn();
            });
            _buttons.Add(btn);
        }

        private System.Collections.IEnumerator AutoPickSingleChoice(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_currentEvent != null && _currentEvent.ChoiceCount == 1)
                OnChoiceSelected(0);
        }

        private System.Collections.IEnumerator CpuPick(PlayerData player, EventMaster ev)
        {
            yield return new WaitForSeconds(1.2f);

            int idx = EventRobustnessValidator.FirstSelectableIndex(ev, player);
            if (idx >= 0)
            {
                OnChoiceSelected(idx);
                yield break;
            }

            if (player.Virtue >= GameConfig.VirtueRescueThreshold)
            {
                player.ApplyStatChange(0, 0, 10, -5);
                CloseAndEndTurn();
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
                if (_panel != null) _panel.SetActive(false);
                _hasVisibleContent = false;
                return;
            }

            EventManager.Instance.ApplyChoice(_currentPlayer, choice);
            CloseAndEndTurn();
        }

        private void CloseAndEndTurn()
        {
            if (_panel != null) _panel.SetActive(false);
            _hasVisibleContent = false;
            EnsureWorldDimmer()?.Hide();
            Board.BoardCameraController.Instance?.RestoreFramedView();
            TurnManager.Instance.EndTurn();
            FindFirstObjectByType<GameHUD>()?.RefreshAll();
        }

        private void BringToFront()
        {
            var canvasRoot = GetComponentInParent<Canvas>()?.transform;
            if (canvasRoot != null)
                transform.SetParent(canvasRoot, false);

            transform.SetAsLastSibling();

            EnsureTopCanvas();
            var cg = EnsureCanvasGroup();
            cg.blocksRaycasts = true;
            cg.interactable   = true;
            cg.alpha          = 1f;
        }

        private void EnsureTopCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private CanvasGroup EnsureCanvasGroup()
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null)
                cg = gameObject.AddComponent<CanvasGroup>();
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
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, EventModalLayout.DisabledAlpha);
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            float height = interactable ? 76f : 84f;
            rt.sizeDelta = new Vector2(680f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight       = height;
            le.preferredWidth  = 680f;

            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(go.transform, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 8, 8);
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = Vector2.zero;
            rowRt.anchorMax = Vector2.one;
            rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(row.transform, false);
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
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.65f);
            labelTmp.raycastTarget = false;
            JapaneseFontProvider.Apply(labelTmp);

            var previewGo = new GameObject("Preview");
            previewGo.transform.SetParent(content.transform, false);
            var previewTmp = previewGo.AddComponent<TextMeshProUGUI>();
            previewTmp.text = EventChoicePreview.FormatRich(c, player);
            previewTmp.fontSize = EventModalLayout.PreviewFontSize;
            previewTmp.alignment = TextAlignmentOptions.Left;
            previewTmp.richText = true;
            previewTmp.raycastTarget = false;
            JapaneseFontProvider.Apply(previewTmp);

            if (!string.IsNullOrEmpty(failReason))
            {
                var failGo = new GameObject("FailReason");
                failGo.transform.SetParent(row.transform, false);
                var failTmp = failGo.AddComponent<TextMeshProUGUI>();
                failTmp.text = EventChoicePreview.FormatFailBadge(failReason);
                failTmp.fontSize = EventModalLayout.PreviewFontSize;
                failTmp.alignment = TextAlignmentOptions.Right;
                failTmp.color = new Color(1f, 0.35f, 0.35f);
                failTmp.richText = true;
                failTmp.raycastTarget = false;
                JapaneseFontProvider.Apply(failTmp);
                var failLe = failGo.AddComponent<LayoutElement>();
                failLe.minWidth = 160f;
                failLe.preferredWidth = 200f;
            }

            return btn;
        }
    }
}
