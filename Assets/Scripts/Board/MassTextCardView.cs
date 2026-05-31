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
        [SerializeField] private Image           accentStrip;
        [SerializeField] private Image           headerBand;
        [SerializeField] private Image           cornerMarker;
        [SerializeField] private Image           titleRule;
        [SerializeField] private TextMeshProUGUI tagText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI markerText;
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
            if (markerText != null)
                markerText.text = "EV";

            var panel  = EventTagColors.GetPanelColor(ev.Tags);
            var accent = EventTagColors.GetBorderColor(ev.Tags);
            if (background != null)
                background.color = panel;
            if (border != null)
                border.color = accent;
            ApplyCardDecoration(panel, accent);
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
            if (markerText != null)
                markerText.text = Waypoint.GetTypeShortLabel(type);

            var panel = EventTagColors.GetSquareTypePanelColor(type);
            if (background != null) background.color = panel;
            var accent = new Color(
                Mathf.Min(panel.r * 1.2f, 1f),
                Mathf.Min(panel.g * 1.2f, 1f),
                Mathf.Min(panel.b * 1.2f, 1f),
                1f);
            if (border != null)
                border.color = accent;
            ApplyCardDecoration(panel, accent);
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

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            background ??= canvas.transform.Find("CardPanel")?.GetComponent<Image>();
            border     ??= canvas.transform.Find("CardBorder")?.GetComponent<Image>();
            tagText    ??= canvas.transform.Find("TagLabel")?.GetComponent<TextMeshProUGUI>();
            titleText  ??= canvas.transform.Find("TitleLabel")?.GetComponent<TextMeshProUGUI>();
            CacheDecorationReferences();
        }

        private void CacheDecorationReferences()
        {
            if (background == null) return;

            var panel = background.transform;
            accentStrip  ??= panel.Find("AccentStrip")?.GetComponent<Image>();
            headerBand   ??= panel.Find("HeaderBand")?.GetComponent<Image>();
            cornerMarker ??= panel.Find("CornerMarker")?.GetComponent<Image>();
            titleRule    ??= panel.Find("TitleRule")?.GetComponent<Image>();
            markerText   ??= panel.Find("MarkerLabel")?.GetComponent<TextMeshProUGUI>();

            accentStrip  ??= CreateDecorationImage(panel, "AccentStrip");
            headerBand   ??= CreateDecorationImage(panel, "HeaderBand");
            cornerMarker ??= CreateDecorationImage(panel, "CornerMarker");
            titleRule    ??= CreateDecorationImage(panel, "TitleRule");
            markerText   ??= CreateDecorationText(panel, "MarkerLabel");

            LayoutDecoration();
            LayoutText();
        }

        private void LayoutDecoration()
        {
            SetRect(accentStrip, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(5.5f, 0f));
            SetRect(headerBand, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 25f), new Vector2(0f, -12.5f));
            SetRect(cornerMarker, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));
            SetRect(markerText, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));
            SetRect(titleRule, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-24f, 3f), new Vector2(8f, 8f));

            accentStrip.transform.SetAsFirstSibling();
            headerBand.transform.SetSiblingIndex(1);
            cornerMarker.transform.SetSiblingIndex(2);
            markerText.transform.SetSiblingIndex(3);
            titleRule.transform.SetSiblingIndex(4);
        }

        private void LayoutText()
        {
            if (tagText != null)
            {
                var rt = tagText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(18f, -5f);
                rt.sizeDelta = new Vector2(-54f, 22f);
            }

            if (titleText != null)
            {
                var rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(18f, 9f);
                rt.offsetMax = new Vector2(-12f, -29f);
            }
        }

        private void ApplyCardDecoration(Color panel, Color accent)
        {
            CacheDecorationReferences();
            if (accentStrip != null) accentStrip.color = accent;
            if (headerBand != null) headerBand.color = WithAlpha(Shade(panel, 0.24f), 0.44f);
            if (cornerMarker != null) cornerMarker.color = WithAlpha(Tint(accent, 0.22f), 0.95f);
            if (titleRule != null) titleRule.color = WithAlpha(Tint(accent, 0.36f), 0.92f);
            if (markerText != null) markerText.color = WithAlpha(Shade(accent, 0.58f), 1f);
        }

        private static Image CreateDecorationImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = BoardVisualUtility.GetSquareSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateDecorationText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 10f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            TitleMenuController.ApplyJapaneseFont(tmp);
            return tmp;
        }

        private static void SetRect(Image img, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (img == null) return;
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetRect(TextMeshProUGUI tmp, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (tmp == null) return;
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static Color Tint(Color c, float amount) => Color.Lerp(c, Color.white, amount);
        private static Color Shade(Color c, float amount) => Color.Lerp(c, Color.black, amount);
        private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

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
