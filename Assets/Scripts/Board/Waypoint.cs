using UnityEngine;
using Sugoroku.Data;
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
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorVisualUpdate();
                return;
            }
#endif
            ApplyPresentation();
        }

        public void RequestVisualUpdate() => RefreshPresentation();
        public void UpdateVisual() => RefreshPresentation();

        private void ApplyPresentation()
        {
            _cardView = GetComponentInChildren<MassTextCardView>(true);
            CacheSpriteRenderer();

            if (_cardView != null)
            {
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
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
            BoardVisualUtility.ApplySpriteRenderer(
                _spriteRenderer,
                BoardSortingLayers.Board,
                BoardSortingLayers.WaypointBaseOrder + routeIndex);

            var style = GetVisualStyle(Type);
            if (_spriteRenderer.sprite != style.sprite)
                _spriteRenderer.sprite = style.sprite;

            _spriteRenderer.color = style.color;
            transform.localScale = Vector3.one * (cellSpacing * style.sizeRatio);
        }

        private static (Sprite sprite, Color color, float sizeRatio) GetVisualStyle(SquareType type) => type switch
        {
            SquareType.Start => (BoardVisualUtility.GetCircleSprite(), new Color(0.35f, 0.85f, 0.45f), 0.50f),
            SquareType.Goal => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.82f, 0.15f), 0.55f),
            SquareType.Normal => (BoardVisualUtility.GetCircleSprite(), new Color(0.82f, 0.82f, 0.84f), 0.42f),
            SquareType.Event => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.88f, 0.2f), 0.50f),
            SquareType.Tuition => (BoardVisualUtility.GetSquareSprite(), new Color(0.95f, 0.25f, 0.25f), 0.46f),
            SquareType.Journal => (BoardVisualUtility.GetCircleSprite(), new Color(0.2f, 0.55f, 1.0f), 0.48f),
            SquareType.Lecture => (BoardVisualUtility.GetCircleSprite(), new Color(0.45f, 0.75f, 0.9f), 0.44f),
            SquareType.Rest => (BoardVisualUtility.GetCircleSprite(), new Color(0.45f, 0.9f, 0.55f), 0.44f),
            SquareType.PartTime => (BoardVisualUtility.GetSquareSprite(), new Color(0.95f, 0.78f, 0.2f), 0.44f),
            SquareType.Bonus => (BoardVisualUtility.GetCircleSprite(), new Color(1.0f, 0.65f, 0.2f), 0.44f),
            SquareType.Penalty => (BoardVisualUtility.GetSquareSprite(), new Color(0.85f, 0.45f, 0.2f), 0.44f),
            SquareType.Branch => (BoardVisualUtility.GetSquareSprite(), new Color(0.55f, 0.85f, 0.45f), 0.52f),
            _ => (BoardVisualUtility.GetCircleSprite(), Color.white, 0.42f),
        };

        public static string GetTypeShortLabel(SquareType type) => type switch
        {
            SquareType.Start   => "ST",
            SquareType.Goal    => "GL",
            SquareType.Event   => "EV",
            SquareType.Tuition => "学費",
            SquareType.Journal => "論文",
            SquareType.Lecture => "ゼミ",
            SquareType.Rest     => "休",
            SquareType.PartTime => "バ",
            SquareType.Bonus    => "バ",
            SquareType.Penalty => "↓",
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
