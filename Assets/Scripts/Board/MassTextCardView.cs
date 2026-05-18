using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Sugoroku.Board
{
    /// <summary>
    /// 横長カード型マス（World Space UI）。EventId から events.json のタイトル・タグを表示。
    /// タップで詳細（説明文）を表示。
    /// </summary>
    [DisallowMultipleComponent]
    public class MassTextCardView : MonoBehaviour
    {
        [SerializeField] private string eventId;
        [SerializeField] private SquareType squareType = SquareType.Normal;
        [SerializeField] private string fallbackTitle = "";

        [Header("UI")]
        [SerializeField] private Image           background;
        [SerializeField] private Image           border;
        [SerializeField] private TextMeshProUGUI tagText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private BoxCollider2D   hitCollider;

        public string EventId => eventId;

        private void Awake() => CacheReferences();

        public void Configure(SquareType type, string boundEventId, string titleFallback)
        {
            squareType     = type;
            eventId        = boundEventId;
            fallbackTitle  = titleFallback ?? "";
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            CacheReferences();

            if (!string.IsNullOrEmpty(eventId))
            {
                var ev = EventMassCatalog.Get(eventId);
                if (ev != null)
                {
                    ApplyEvent(ev);
                    return;
                }
            }

            ApplySquareType(squareType, fallbackTitle);
        }

        private void ApplyEvent(EventMaster ev)
        {
            if (tagText != null)
            {
                tagText.text = EventTagColors.FormatTags(ev.Tags);
                tagText.color = new Color(0.92f, 0.94f, 1f, 0.95f);
            }

            if (titleText != null)
            {
                titleText.text = ev.Title ?? "";
                titleText.color = Color.white;
            }

            if (background != null)
                background.color = EventTagColors.GetPanelColor(ev.Tags);
            if (border != null)
                border.color = EventTagColors.GetBorderColor(ev.Tags);
        }

        private void ApplySquareType(SquareType type, string title)
        {
            string tag = type switch
            {
                SquareType.Start   => "スタート",
                SquareType.Goal    => "ゴール",
                SquareType.Tuition => "学費",
                SquareType.Journal => "論文",
                SquareType.Lecture => "ゼミ",
                SquareType.Rest     => "休息",
                SquareType.PartTime => "バイト",
                SquareType.Bonus    => "チャンス",
                SquareType.Penalty => "ペナルティ",
                SquareType.Event   => "イベント",
                _                  => "通常",
            };

            if (tagText != null)
            {
                tagText.text  = $"[{tag}]";
                tagText.color = new Color(0.9f, 0.92f, 0.96f, 0.9f);
            }

            if (titleText != null)
            {
                titleText.text  = string.IsNullOrEmpty(title) ? tag : title;
                titleText.color = Color.white;
                titleText.fontSize = Mathf.Max(titleText.fontSize, 14f);
            }

            var panel = EventTagColors.GetSquareTypePanelColor(type);
            if (background != null) background.color = panel;
            if (border != null)
                border.color = new Color(panel.r * 1.2f, panel.g * 1.2f, panel.b * 1.2f, 1f);
        }

        private void Update()
        {
            if (EventModalUI.HasVisibleModal) return;
            if (IsPointerOverUi()) return;
            if (!TryGetClickWorldPoint(out var world)) return;
            if (hitCollider == null) hitCollider = GetComponent<BoxCollider2D>();
            if (hitCollider == null || !hitCollider.OverlapPoint(world)) return;

            ShowDetail();
        }

        private void ShowDetail()
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                var ev = EventMassCatalog.Get(eventId);
                if (ev != null)
                {
                    EventModalUI.ShowPreview(ev);
                    return;
                }
            }

            EventModalUI.ShowSquarePreview(squareType, fallbackTitle,
                "このマスに止まると、ゲーム内のルールに従って効果が発動します。");
        }

        private void CacheReferences()
        {
            hitCollider ??= GetComponent<BoxCollider2D>();
            EnsureCanvasRenders();

            if (background != null && tagText != null) return;

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            background ??= canvas.transform.Find("CardPanel")?.GetComponent<Image>();
            border     ??= canvas.transform.Find("CardBorder")?.GetComponent<Image>();
            tagText    ??= canvas.transform.Find("TagLabel")?.GetComponent<TextMeshProUGUI>();
            titleText  ??= canvas.transform.Find("TitleLabel")?.GetComponent<TextMeshProUGUI>();
        }

        private void EnsureCanvasRenders()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            canvas.overrideSorting = true;
            if (SortingLayerExists(BoardSortingLayers.Board))
                canvas.sortingLayerName = BoardSortingLayers.Board;
            if (canvas.sortingOrder < BoardSortingLayers.WaypointBaseOrder)
                canvas.sortingOrder = BoardSortingLayers.WaypointBaseOrder + 80;
        }

        private static bool SortingLayerExists(string layerName)
        {
            foreach (var layer in SortingLayer.layers)
                if (layer.name == layerName) return true;
            return false;
        }

        private static bool TryGetClickWorldPoint(out Vector3 world)
        {
            world = Vector3.zero;
            var cam = Camera.main;
            if (cam == null) return false;

            Vector2? screen = null;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                screen = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.wasPressedThisFrame) screen = t.position.ReadValue();
            }

            if (screen == null) return false;
            var p = screen.Value;
            world = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
            return true;
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                eventSystem.IsPointerOverGameObject())
                return true;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame &&
                    eventSystem.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    return true;
            }

            return false;
        }
    }
}
