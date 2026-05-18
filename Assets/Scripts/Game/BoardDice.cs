using System;
using System.Collections;
using UnityEngine;
using Sugoroku.Board;
using Sugoroku.Visual;
using UnityEngine.InputSystem;

namespace Sugoroku.Game
{
    /// <summary>
    /// ワールド上のサイコロ表示。クリックで振る／CPU は DiceRoller 経由で同じアニメーションを再生。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BoardDice : MonoBehaviour
    {
        public static BoardDice Instance { get; private set; }

        [SerializeField] private string diceSpritePrefix = "dieWhite";
        [SerializeField] private float worldScale = 1.2f;
        [SerializeField] private float hopHeight = 0.45f;

        private Sprite[] _diceSides;
        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Color _baseColor;
        private bool _animating;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _renderer = GetComponent<SpriteRenderer>();
            BoardVisualUtility.ApplySpriteRenderer(_renderer, BoardSortingLayers.Player, 80);

            if (GetComponent<Collider2D>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * 1.1f;
            }

            transform.localScale = Vector3.one * worldScale;
            _baseScale    = transform.localScale;
            _basePosition = transform.position;
            _baseColor    = Color.white;
            LoadSprites();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LoadSprites()
        {
            _diceSides = KenneyAssets.LoadDiceFaces(diceSpritePrefix);
            if (_diceSides.Length >= 6)
                _renderer.sprite = _diceSides[5];
        }

        private void Update()
        {
            if (_animating || DiceRoller.Instance == null) return;
            if (!TryGetClickWorldPoint(out var worldPoint)) return;

            var hit = Physics2D.OverlapPoint(worldPoint);
            if (hit == null || hit.gameObject != gameObject) return;
            if (!DiceRoller.Instance.CanRoll()) return;

            DiceRoller.Instance.Roll();
        }

        public IEnumerator PlayRollAnimation(int finalValue, Action<int> onFaceShown = null)
        {
            if (_diceSides == null || _diceSides.Length < 6)
            {
                onFaceShown?.Invoke(finalValue);
                yield break;
            }

            _animating = true;
            _basePosition = transform.position;

            yield return DiceJuice.PlaySpriteRoll(
                _diceSides,
                finalValue,
                sp =>
                {
                    if (sp != null) _renderer.sprite = sp;
                },
                onFaceShown,
                ApplyJuiceState,
                TransformJuiceState.Identity);

            transform.position   = _basePosition;
            transform.localScale = _baseScale;
            transform.rotation   = Quaternion.identity;
            _renderer.color      = _baseColor;
            _animating = false;
        }

        private void ApplyJuiceState(TransformJuiceState state)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, state.rotationZ);
            float s = state.scaleMul;
            float sq = Mathf.Max(0.01f, state.scaleSquash);
            transform.localScale = new Vector3(
                _baseScale.x * s / sq,
                _baseScale.y * s * sq,
                _baseScale.z);

            transform.position = _basePosition + Vector3.up * (state.offsetY * hopHeight);

            float f = state.flash;
            _renderer.color = new Color(f, f, f, 1f);
        }

        public bool IsAnimating => _animating;

        private static bool TryGetClickWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            var cam = Camera.main;
            if (cam == null) return false;

            Vector2? screenPos = null;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                screenPos = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                    screenPos = touch.position.ReadValue();
            }

            if (screenPos == null) return false;

            var p = screenPos.Value;
            worldPoint = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
            return true;
        }
    }
}
