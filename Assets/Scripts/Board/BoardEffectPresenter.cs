using System.Collections;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;
using Sugoroku.UI;

namespace Sugoroku.Board
{
    /// <summary>盤面上のターン・移動・マス効果を、軽量なスプライト演出で見せる。</summary>
    public class BoardEffectPresenter : MonoBehaviour
    {
        public static BoardEffectPresenter Instance { get; private set; }

        [SerializeField] private float _turnRingDuration = 0.42f;
        [SerializeField] private float _stepRingDuration = 0.28f;
        [SerializeField] private float _squarePulseDuration = 0.44f;

        private bool _subscribed;

        public static BoardEffectPresenter EnsureSceneInstance()
        {
            if (Instance != null) return Instance;

            var parent = BoardManager.Instance != null ? BoardManager.Instance.transform : null;
            var go = new GameObject("BoardEffectPresenter");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<BoardEffectPresenter>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (!TrySubscribe())
                StartCoroutine(SubscribeWhenReady());
        }

        private void OnDisable() => Unsubscribe();

        private IEnumerator SubscribeWhenReady()
        {
            while (TurnManager.Instance == null || GameManager.Instance == null)
                yield return null;

            TrySubscribe();
        }

        private bool TrySubscribe()
        {
            if (_subscribed) return true;
            if (TurnManager.Instance == null || GameManager.Instance == null) return false;
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
            TurnManager.Instance.OnPlayerMoved += HandlePlayerMoved;
            GameManager.Instance.OnSquareEffect += HandleSquareEffect;
            _subscribed = true;
            return true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
                TurnManager.Instance.OnPlayerMoved -= HandlePlayerMoved;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnSquareEffect -= HandleSquareEffect;
            _subscribed = false;
        }

        private void HandleTurnStarted(PlayerData player)
        {
            var piece = GetPiece(player);
            if (piece == null) return;

            var pos = piece.transform.position;
            var color = player != null ? player.PieceTint : Color.white;
            StartCoroutine(RingPulse(pos, color, 0.45f, 1.65f,
                GameConfig.AnimationDuration(_turnRingDuration), BoardSortingLayers.WaypointBaseOrder + 120));
            StartCoroutine(SparkBurst(pos, color, 8, 0.42f, GameConfig.AnimationDuration(0.34f)));
            FloatingTextUI.Instance?.Show(player != null && player.IsCpu ? "CPUターン" : "ターン開始",
                pos + Vector3.up * 1.05f, color);
        }

        private void HandlePlayerMoved(PlayerData player, int boardPosition)
        {
            var pos = BoardManager.Instance != null
                ? BoardManager.Instance.GetPosition(boardPosition)
                : (GetPiece(player)?.transform.position ?? Vector3.zero);
            var color = GetSquareColor(BoardManager.Instance?.GetSquareType(boardPosition) ?? SquareType.Normal);
            StartCoroutine(RingPulse(pos, color, 0.55f, 2.0f,
                GameConfig.AnimationDuration(_squarePulseDuration), BoardSortingLayers.WaypointBaseOrder + 95));
        }

        private void HandleSquareEffect(PlayerData player, string message)
        {
            if (player == null || BoardManager.Instance == null) return;

            var type = BoardManager.Instance.GetSquareType(player.BoardPosition);
            bool ignored = !string.IsNullOrEmpty(message) && message.Contains("回避");
            var color = ignored ? new Color(0.72f, 0.74f, 0.78f, 0.95f) : GetSquareColor(type);
            var pos = BoardManager.Instance.GetPosition(player.BoardPosition);

            StartCoroutine(SquarePulse(pos, color, ignored));
            StartCoroutine(SparkBurst(pos, color, ignored ? 6 : 12, ignored ? 0.36f : 0.62f,
                GameConfig.AnimationDuration(0.42f)));
            FloatingTextUI.Instance?.Show(ignored ? "回避" : SquareEffectLabels.Get(type),
                pos + Vector3.up * 1.0f, color);
            BoardCameraController.ShakeInstance(ignored ? 0.03f : 0.05f, ignored ? 0.08f : 0.12f);
        }

        public void PlayStepLanding(PlayerData player, Vector3 worldPos)
        {
            var color = player != null ? player.PieceTint : Color.white;
            StartCoroutine(RingPulse(worldPos, color, 0.24f, 0.95f,
                GameConfig.AnimationDuration(_stepRingDuration), BoardSortingLayers.WaypointBaseOrder + 100));
            StartCoroutine(SparkBurst(worldPos, color, 5, 0.26f, GameConfig.AnimationDuration(0.22f)));
        }

        private IEnumerator RingPulse(Vector3 pos, Color color, float startSize, float endSize, float duration, int order)
        {
            var sr = CreateEffectSprite("Effect_Ring", pos, BoardVisualUtility.GetCircleSprite(), color, order);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = JuiceMath.EaseOutQuad(t);
                SetWorldSize(sr.transform, Mathf.Lerp(startSize, endSize, eased));
                sr.color = WithAlpha(color, (1f - t) * 0.42f);
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        private IEnumerator SquarePulse(Vector3 pos, Color color, bool muted)
        {
            var sr = CreateEffectSprite("Effect_SquarePulse", pos, BoardVisualUtility.GetSquareSprite(), color,
                BoardSortingLayers.WaypointBaseOrder + 92);
            float baseW = MassTextCardPrefabFactory.CardWorldWidth + 0.18f;
            float baseH = MassTextCardPrefabFactory.CardWorldHeight + 0.16f;
            float grow = muted ? 0.28f : 0.48f;
            float elapsed = 0f;
            float duration = GameConfig.AnimationDuration(_squarePulseDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = JuiceMath.EaseOutQuad(t);
                SetWorldSize(sr.transform, baseW + grow * eased, baseH + grow * eased);
                sr.color = WithAlpha(color, (1f - t) * (muted ? 0.20f : 0.34f));
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        private IEnumerator SparkBurst(Vector3 pos, Color color, int count, float radius, float duration)
        {
            var sparks = new SpriteRenderer[count];
            var dirs = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i / count) + (i % 2) * 0.22f;
                dirs[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                sparks[i] = CreateEffectSprite("Effect_Spark", pos + dirs[i] * 0.12f,
                    BoardVisualUtility.GetCircleSprite(), color, BoardSortingLayers.WaypointBaseOrder + 135);
                SetWorldSize(sparks[i].transform, 0.10f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = JuiceMath.EaseOutQuad(t);
                for (int i = 0; i < sparks.Length; i++)
                {
                    if (sparks[i] == null) continue;
                    sparks[i].transform.position = pos + dirs[i] * Mathf.Lerp(0.12f, radius, eased);
                    sparks[i].color = WithAlpha(color, 1f - t);
                }
                yield return null;
            }

            foreach (var spark in sparks)
                if (spark != null) Destroy(spark.gameObject);
        }

        private SpriteRenderer CreateEffectSprite(string name, Vector3 pos, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Board, order);
            return sr;
        }

        private static GameObject GetPiece(PlayerData player) =>
            player != null ? GameManager.Instance?.GetPiece(player.Index) : null;

        private static Color GetSquareColor(SquareType type) => EventTagColors.GetSquareTypePanelColor(type);

        private static void SetWorldSize(Transform t, float size) => SetWorldSize(t, size, size);

        private static void SetWorldSize(Transform t, float width, float height)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            var spriteSize = sr != null && sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            t.localScale = new Vector3(
                width / Mathf.Max(spriteSize.x, 0.01f),
                height / Mathf.Max(spriteSize.y, 0.01f),
                1f);
        }

        private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
