using UnityEngine;
using Sugoroku.Data;
using Sugoroku.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sugoroku.Board
{
    /// <summary>
    /// マス目のゲームロジック。表示は <see cref="MassTextCardView"/> が担当（あれば）。
    /// </summary>
    [DisallowMultipleComponent]
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private int routeIndex;
        [SerializeField] private string displayName;
        [SerializeField] private string boundEventId;
        [SerializeField] private float cellSpacing = 2f;

        [Header("マス設定")]
        public SquareType Type = SquareType.Normal;

        [Header("学費マス設定")]
        public int TuitionAmount = GameConfig.TuitionCost;

        [Header("ジャーナルマス設定")]
        public int JournalIfGain = 10;

        [Header("休憩マス設定")]
        public int RestMentalGain = 20;

        [Header("ボーナス/ペナルティ")]
        public int BonusMoney   = 5;
        public int PenaltyMoney = -5;

        public int RouteIndex => routeIndex;
        public string DisplayName => displayName;
        public string BoundEventId => boundEventId;

        private SpriteRenderer _spriteRenderer;
        private MassTextCardView _cardView;

#if UNITY_EDITOR
        private bool _editorVisualUpdateQueued;
#endif

        public void SetCellSpacing(float spacing)
        {
            if (spacing > 0.01f) cellSpacing = spacing;
        }

        public void SetRouteIndex(int index)
        {
            routeIndex = index;
            RefreshPresentation();
        }

        public void Configure(int index, SquareType type, string label = null, string eventId = null)
        {
            ApplyRouteData(index, type, label, eventId);
            RefreshPresentation();
        }

        public void ApplyRouteData(int index, SquareType type, string label = null, string eventId = null)
        {
            routeIndex   = index;
            Type         = type;
            displayName  = label ?? SnakeBoardLayout.GetDisplayLabel(index);
            boundEventId = eventId ?? SnakeBoardLayout.GetEventId(index);
            name         = BuildObjectName(index, type);
        }

        public EventMaster ResolveBoundEvent()
        {
            if (string.IsNullOrEmpty(boundEventId)) return null;
            EventMassCatalog.EnsureLoaded();
            var ev = EventMassCatalog.Get(boundEventId);
            if (ev != null) return ev;
            return Sugoroku.Game.EventManager.Instance?.GetById(boundEventId);
        }

        public void RefreshPresentation()
        {
            ApplyPresentation();
        }

        public void RequestVisualUpdate() => RefreshPresentation();
        public void UpdateVisual() => RefreshPresentation();

        /// <summary>分岐ルート（分岐点・両レーン）を水色のソリッドカードにする。</summary>
        public void ApplyBranchRouteStyle()
        {
            if (_cardView == null) _cardView = GetComponentInChildren<MassTextCardView>(true);
            if (_cardView != null) _cardView.SetBranchRouteStyle(true);
        }

        private SpriteRenderer _ringRenderer;

        private void ApplyPresentation()
        {
            _cardView = GetComponentInChildren<MassTextCardView>(true);
            CacheSpriteRenderer();

            if (_cardView != null)
            {
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
                var ringTr = transform.Find("WaypointPinRing");
                if (ringTr != null) ringTr.gameObject.SetActive(false);
                var labelTr = transform.Find("WaypointPinLabel");
                if (labelTr != null) labelTr.gameObject.SetActive(false);
                _cardView.Configure(Type, boundEventId, displayName);
                return;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
                ApplyLegacySpriteVisual();
            }
        }

        private void ApplyLegacySpriteVisual()
        {
            int baseOrder = BoardSortingLayers.WaypointBaseOrder + routeIndex * 4;

            var style = GetVisualStyle(Type);
            if (_spriteRenderer.sprite == null || _spriteRenderer.sprite != style.sprite)
                _spriteRenderer.sprite = style.sprite != null ? style.sprite : BoardVisualUtility.GetCircleSprite();

            _spriteRenderer.color = style.color;

            // スプライトのネイティブサイズ（128px / 32PPU = 4.0f units）
            float nativeSize = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size.x : 4.0f;
            if (nativeSize <= 0.01f) nativeSize = 4.0f;

            // マスの直径: 約 0.92 unit（隣マスとの間に約 0.9 unit の適度な隙間を確保し、イラストを活かす）
            float desiredDiameter = Mathf.Min(cellSpacing * 0.50f, 0.92f) * (style.sizeRatio / 0.50f);
            float scale = desiredDiameter / nativeSize;
            transform.localScale = Vector3.one * scale;

            EnsureRingRenderer(baseOrder);
            BoardVisualUtility.ApplySpriteRenderer(
                _spriteRenderer,
                BoardSortingLayers.Board,
                baseOrder + 1);

            UpdatePinLabel(baseOrder + 2);
        }

        private void EnsureRingRenderer(int baseOrder)
        {
            if (_ringRenderer == null)
            {
                var ringTr = transform.Find("WaypointPinRing");
                if (ringTr != null) _ringRenderer = ringTr.GetComponent<SpriteRenderer>();
                else
                {
                    var ringGo = new GameObject("WaypointPinRing");
                    ringGo.transform.SetParent(transform, false);
                    ringGo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                    ringGo.transform.localScale = Vector3.one * 1.22f;
                    _ringRenderer = ringGo.AddComponent<SpriteRenderer>();
                }
            }
            if (_ringRenderer != null)
            {
                _ringRenderer.gameObject.SetActive(true);
                _ringRenderer.sprite = BoardVisualUtility.GetCircleSprite();
                _ringRenderer.color = new Color(1f, 1f, 1f, 0.94f);
                BoardVisualUtility.ApplySpriteRenderer(_ringRenderer, BoardSortingLayers.Board, baseOrder);
            }
        }

        private void UpdatePinLabel(int order)
        {
            var labelTr = transform.Find("WaypointPinLabel");
            TMPro.TextMeshPro tmp = null;
            if (labelTr != null)
            {
                labelTr.gameObject.SetActive(true);
                tmp = labelTr.GetComponent<TMPro.TextMeshPro>();
            }
            else
            {
                var labelGo = new GameObject("WaypointPinLabel");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                labelGo.transform.localScale = Vector3.one;
                tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            }

            if (tmp != null)
            {
                var font = JapaneseFontProvider.Get();
                if (font != null) tmp.font = font;

                string text = GetTypeShortLabel(Type);
                tmp.text = text;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 4f;
                tmp.fontSizeMax = 8.5f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                bool isLightBackground = Type == SquareType.Start || Type == SquareType.Normal ||
                                         Type == SquareType.Goal  || Type == SquareType.Event  ||
                                         Type == SquareType.Rest;
                tmp.color = isLightBackground
                    ? new Color(0.10f, 0.14f, 0.22f, 0.98f)
                    : Color.white;
                tmp.sortingLayerID = SortingLayer.NameToID(BoardSortingLayers.Board);
                tmp.sortingOrder = order;
                tmp.rectTransform.sizeDelta = new Vector2(3.5f, 3.5f);
            }
        }

        private static (Sprite sprite, Color color, float sizeRatio) GetVisualStyle(SquareType type) => type switch
        {
            SquareType.Start => (BoardVisualUtility.GetCircleSprite(), new Color(0.28f, 0.82f, 0.40f), 0.52f),
            SquareType.Goal => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.80f, 0.15f), 0.54f),
            SquareType.Normal => (BoardVisualUtility.GetCircleSprite(), new Color(0.92f, 0.92f, 0.94f), 0.46f),
            SquareType.Event => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.86f, 0.22f), 0.48f),
            SquareType.Tuition => (BoardVisualUtility.GetCircleSprite(), new Color(0.95f, 0.28f, 0.28f), 0.48f),
            SquareType.Journal => (BoardVisualUtility.GetCircleSprite(), new Color(0.22f, 0.58f, 0.96f), 0.48f),
            SquareType.Lecture => (BoardVisualUtility.GetCircleSprite(), new Color(0.40f, 0.75f, 0.92f), 0.48f),
            SquareType.Rest => (BoardVisualUtility.GetCircleSprite(), new Color(0.40f, 0.88f, 0.60f), 0.48f),
            SquareType.PartTime => (BoardVisualUtility.GetCircleSprite(), new Color(0.98f, 0.72f, 0.22f), 0.48f),
            SquareType.Bonus => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.65f, 0.20f), 0.48f),
            SquareType.Penalty => (BoardVisualUtility.GetCircleSprite(), new Color(0.88f, 0.40f, 0.25f), 0.48f),
            SquareType.Branch => (BoardVisualUtility.GetCircleSprite(), new Color(0.45f, 0.82f, 0.50f), 0.52f),
            _ => (BoardVisualUtility.GetCircleSprite(), Color.white, 0.46f),
        };

        public static string GetTypeShortLabel(SquareType type) => type switch
        {
            SquareType.Start   => "START",
            SquareType.Goal    => "GOAL",
            SquareType.Event   => "？",
            SquareType.Tuition => "学費",
            SquareType.Journal => "論文",
            SquareType.Lecture => "ゼミ",
            SquareType.Rest     => "休",
            SquareType.PartTime => "バイト",
            SquareType.Bonus    => "＋",
            SquareType.Penalty => "−",
            SquareType.Branch  => "分岐",
            _                  => ""
        };

        private static string BuildObjectName(int index, SquareType type)
        {
            var shortLabel = GetTypeShortLabel(type);
            return string.IsNullOrEmpty(shortLabel) ? $"W{index:D2}" : $"W{index:D2}_{shortLabel}";
        }

        public void RemoveWorldLabels()
        {
            foreach (var tmp in GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                if (tmp.GetComponentInParent<MassTextCardView>() != null) continue;
                if (tmp.gameObject.name == "WaypointPinLabel") continue;
                DestroyLabelObject(tmp.gameObject);
            }
        }

        private static void DestroyLabelObject(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        private void CacheSpriteRenderer()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Reset() => RemoveWorldLabels();

        private void Awake()
        {
            RemoveWorldLabels();
            CacheSpriteRenderer();
        }

        private void Start() => ApplyPresentation();

        private void OnValidate() { }

#if UNITY_EDITOR
        private void QueueEditorVisualUpdate()
        {
            if (_editorVisualUpdateQueued) return;
            _editorVisualUpdateQueued = true;
            EditorApplication.delayCall += OnEditorDelayedVisualUpdate;
        }

        private void OnEditorDelayedVisualUpdate()
        {
            _editorVisualUpdateQueued = false;
            EditorApplication.delayCall -= OnEditorDelayedVisualUpdate;
            if (this == null) return;
            ApplyPresentation();
        }
#endif
    }
}
