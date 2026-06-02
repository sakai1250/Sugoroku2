using System.Collections;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.Board
{
    /// <summary>盤面フレーム・駒追従・イベントズーム（requirements §2.2）。</summary>
    [DisallowMultipleComponent]
    public class BoardCameraController : MonoBehaviour
    {
        public static BoardCameraController Instance { get; private set; }

        [SerializeField] private Camera       _camera;
        [SerializeField] private BoardManager _boardManager;
        [SerializeField] private float        _padding = 2f;
        [SerializeField] private float        _cameraZ = -10f;

        [Header("追従・ズーム")]
        [SerializeField] private float _followSmooth     = 7f;
        [SerializeField] private float _eventZoomScale   = 0.72f;
        [SerializeField] private float _eventFocusOffset = 0.4f;
        [SerializeField] private float _diceLookaheadBlend = 0.42f;
        [SerializeField] private float _waitOrthoScaleRatio = 0.98f;
        [SerializeField] private float _waitViewBoardBlend = 0.45f;

        [Header("疑似3D")]
        [SerializeField] private bool  _useFauxDepthBanking = true;
        [SerializeField] private float _maxBankDegrees = 1.35f;
        [SerializeField] private float _bankSmooth = 8f;

        private Vector3 _framedPosition;
        private Vector3 _boardCenter;
        private float   _boardOrthoSize;
        private bool    _isFollowing;
        private Vector3 _followTarget;
        private Coroutine _shakeRoutine;
        private Coroutine _zoomRoutine;
        private Quaternion _baseRotation;
        private Vector3 _lastCameraPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (_camera == null) _camera = GetComponent<Camera>();
            if (_boardManager == null) _boardManager = FindFirstObjectByType<BoardManager>();
            _framedPosition = transform.position;
            _baseRotation = transform.rotation;
            _lastCameraPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnStateChanged -= HandleTurnState;
                TurnManager.Instance.OnTurnStarted  -= FocusCurrentPlayer;
            }
        }

        public static void ShakeInstance(float intensity = 0.1f, float duration = 0.18f)
        {
            Instance?.Shake(intensity, duration);
        }

        public void Shake(float intensity, float duration)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeCoroutine(intensity, GameConfig.AnimationDuration(duration)));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            var origin = _framedPosition != Vector3.zero ? _framedPosition : transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - elapsed / duration;
                float phase = elapsed / GameConfig.AnimationDurationScale;
                float x = (Mathf.PerlinNoise(phase * 40f, 0f) - 0.5f) * 2f * intensity * t;
                float y = (Mathf.PerlinNoise(0f, phase * 40f) - 0.5f) * 2f * intensity * t;
                transform.position = origin + new Vector3(x, y, 0f);
                yield return null;
            }
            transform.position = origin;
            _shakeRoutine = null;
        }

        private void Start()
        {
            FrameBoard();
            StartCoroutine(BindTurnManagerWhenReady());
        }

        private IEnumerator BindTurnManagerWhenReady()
        {
            while (TurnManager.Instance == null) yield return null;
            TurnManager.Instance.OnStateChanged += HandleTurnState;
            TurnManager.Instance.OnTurnStarted  += FocusCurrentPlayer;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            if (_isFollowing)
            {
                var pos = transform.position;
                transform.position = Vector3.Lerp(pos, _followTarget, _followSmooth * Time.deltaTime);
                _framedPosition = transform.position;
            }

            ApplyFauxDepthBank();
        }

        public void FollowPosition(Vector3 worldPos)
        {
            _isFollowing  = true;
            _followTarget = new Vector3(worldPos.x, worldPos.y, _cameraZ);
        }

        public void StopFollowing() => _isFollowing = false;

        public void FocusCurrentPlayer(PlayerData player)
        {
            if (player == null || GameManager.Instance == null) return;
            var piece = GameManager.Instance.GetPiece(player.Index);
            if (piece == null) return;

            if (_boardOrthoSize <= 0f)
                FrameBoard();

            BoardDicePlacement.PlaceNearPlayer(player);
            FocusForWaitAction(player, piece.transform.position);
        }

        /// <summary>行動待ち: 駒・サイコロ・盤面が同時に見える引きのカメラ。</summary>
        public void FocusForWaitAction(PlayerData player, Vector3 pieceWorldPos)
        {
            if (_camera == null) return;
            if (_boardOrthoSize <= 0f) FrameBoard();

            Vector3 dicePos = BoardDicePlacement.GetDiceWorldPosition(player);
            Vector3 focus = Vector3.Lerp(
                (pieceWorldPos + dicePos) * 0.5f,
                _boardCenter,
                _waitViewBoardBlend);

            var targetPos = new Vector3(focus.x, focus.y, _cameraZ);
            transform.position = targetPos;
            _framedPosition = targetPos;
            _followTarget = targetPos;
            _camera.orthographicSize = _boardOrthoSize * _waitOrthoScaleRatio;
            _isFollowing = false;
        }

        /// <summary>ダイスロール中、進行方向のマスを少し先読み（§6.3）。</summary>
        public void PreviewDiceRoll(int steps, PlayerData player)
        {
            if (player == null || _boardManager == null || steps <= 0) return;
            var piece = GameManager.Instance?.GetPiece(player.Index);
            if (piece == null) return;

            int board = _boardManager.BoardSize;
            int target = Mathf.Min(player.BoardPosition + steps, board - 1);
            Vector3 from = piece.transform.position;
            Vector3 ahead = _boardManager.GetPosition(target);
            float blend = GameConfig.CameraDiceLookaheadBlend > 0
                ? GameConfig.CameraDiceLookaheadBlend
                : _diceLookaheadBlend;
            Vector3 blended = Vector3.Lerp(from, ahead, blend);
            FollowPosition(blended);
        }

        public void ZoomForEvent(Vector3 worldPos)
        {
            if (_zoomRoutine != null) StopCoroutine(_zoomRoutine);
            _zoomRoutine = StartCoroutine(ZoomCoroutine(worldPos, _boardOrthoSize * _eventZoomScale));
        }

        public void RestoreFramedView()
        {
            if (_zoomRoutine != null) StopCoroutine(_zoomRoutine);
            _zoomRoutine = StartCoroutine(ZoomCoroutine(_boardCenter, _boardOrthoSize, restoreFollow: true));
        }

        private void HandleTurnState(TurnState state)
        {
            var player = GameManager.Instance?.GetCurrentPlayer();
            if (player == null) return;

            switch (state)
            {
                case TurnState.Moving:
                    _isFollowing = true;
                    break;
                case TurnState.Event:
                case TurnState.Apply:
                    StopFollowing();
                    break;
                case TurnState.WaitAction:
                    if (_zoomRoutine != null)
                    {
                        StopCoroutine(_zoomRoutine);
                        _zoomRoutine = null;
                    }
                    FocusCurrentPlayer(player);
                    break;
                case TurnState.TurnEnd:
                    FocusCurrentPlayer(player);
                    break;
            }
        }

        private IEnumerator ZoomCoroutine(Vector3 focusWorld, float targetOrtho, bool restoreFollow = false)
        {
            if (_camera == null) yield break;

            Vector3 targetPos = new Vector3(focusWorld.x, focusWorld.y + _eventFocusOffset, _cameraZ);
            float startOrtho = _camera.orthographicSize;
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            float dur = GameConfig.AnimationDuration(0.35f);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                _camera.orthographicSize = Mathf.Lerp(startOrtho, targetOrtho, t);
                yield return null;
            }

            transform.position = targetPos;
            _camera.orthographicSize = targetOrtho;
            _framedPosition = targetPos;
            if (restoreFollow)
            {
                var p = GameManager.Instance?.GetCurrentPlayer();
                if (p != null) FocusCurrentPlayer(p);
            }
            _zoomRoutine = null;
        }

        public void FrameBoard()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _boardManager == null) return;

            Waypoint[] waypoints = null;
            if (_boardManager.Route != null && _boardManager.Route.Count > 0)
            {
                var list = _boardManager.Route.Waypoints;
                waypoints = new Waypoint[list.Count];
                for (int i = 0; i < list.Count; i++) waypoints[i] = list[i];
            }
            else
                waypoints = _boardManager.GetComponentsInChildren<Waypoint>(true);

            if (waypoints == null || waypoints.Length == 0) return;

            var bounds = BoardVisualUtility.CalculateWaypointBounds(waypoints, _padding);
            _camera.orthographic = true;
            _camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, _cameraZ);

            float halfHeight = Mathf.Max(bounds.extents.y, 4f);
            float halfWidth  = Mathf.Max(bounds.extents.x / Mathf.Max(_camera.aspect, 0.1f), 4f);
            _camera.orthographicSize = Mathf.Max(halfHeight, halfWidth);
            _boardOrthoSize = _camera.orthographicSize;
            _boardCenter    = bounds.center;
            _framedPosition  = _camera.transform.position;
            _lastCameraPosition = _camera.transform.position;
        }

        private void ApplyFauxDepthBank()
        {
            if (!_useFauxDepthBanking)
            {
                transform.rotation = _baseRotation;
                _lastCameraPosition = transform.position;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var velocity = (transform.position - _lastCameraPosition) / dt;
            float targetBank = _isFollowing
                ? Mathf.Clamp(-velocity.x * 0.18f, -_maxBankDegrees, _maxBankDegrees)
                : 0f;

            var targetRotation = _baseRotation * Quaternion.Euler(0f, 0f, targetBank);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _bankSmooth * dt);
            _lastCameraPosition = transform.position;
        }
    }
}
