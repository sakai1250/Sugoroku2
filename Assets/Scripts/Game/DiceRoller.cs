using System;
using System.Collections;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Board;
using Sugoroku.UI;
using Sugoroku.Network;

namespace Sugoroku.Game
{
    /// <summary>
    /// 出目の決定とターン進行の橋渡し。出目は <see cref="NetworkSessionHost"/>（StateAuthority）経由（§6.1 / §7.3）。
    /// </summary>
    public class DiceRoller : MonoBehaviour
    {
        public static DiceRoller Instance { get; private set; }

        public event Action<int> OnRollComplete;
        public event Action<int> OnRollFaceChanged;

        public bool IsRolling { get; private set; }

        [SerializeField] private BoardDice       _boardDice;
        [SerializeField] private DiceHudAnimator _hudAnimator;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_boardDice == null)
                _boardDice = FindFirstObjectByType<BoardDice>();
            if (_hudAnimator == null)
                _hudAnimator = FindFirstObjectByType<DiceHudAnimator>();
        }

        public bool CanRoll()
        {
            if (!CanRollCurrentTurn()) return false;
            var net = NetworkSessionHost.Instance;
            if (net != null) return net.CanLocalPlayerRoll();
            var player = GameManager.Instance.GetCurrentPlayer();
            return player != null && !player.IsCpu;
        }

        private bool CanRollCurrentTurn()
        {
            if (IsRolling) return false;
            if (GameManager.Instance == null || !GameManager.Instance.IsInitialized) return false;
            if (TurnManager.Instance == null) return false;
            if (TurnManager.Instance.CurrentState != TurnState.WaitAction) return false;

            var player = GameManager.Instance.GetCurrentPlayer();
            return player != null && !player.IsFinished;
        }

        public void Roll(bool requireLocalHuman = false)
        {
            if (requireLocalHuman)
            {
                if (!CanRoll()) return;
            }
            else if (!CanRollCurrentTurn())
                return;

            var net = NetworkSessionHost.Instance;
            int result = net != null
                ? net.RequestDiceRoll()
                : UnityEngine.Random.Range(GameConfig.MinDice, GameConfig.MaxDice + 1);

            if (result < GameConfig.MinDice)
                return;

            StartCoroutine(RollCoroutine(result));
        }

        /// <summary>RPC 同期用（オンライン時は全クライアントで同じ出目アニメーション）。</summary>
        public void PlaySyncedRoll(int result)
        {
            if (IsRolling || result < GameConfig.MinDice) return;
            StartCoroutine(RollCoroutine(result));
        }

        private IEnumerator RollCoroutine(int result)
        {
            IsRolling = true;
            var player = GameManager.Instance?.GetCurrentPlayer();
            BoardCameraController.Instance?.PreviewDiceRoll(result, player);
            Audio.GameAudioController.Instance?.PlayDiceRoll();

            void OnFace(int v)
            {
                OnRollFaceChanged?.Invoke(v);
                OnRollComplete?.Invoke(v);
            }

            var board = _boardDice != null ? _boardDice : BoardDice.Instance;
            var hud   = _hudAnimator != null ? _hudAnimator : FindFirstObjectByType<DiceHudAnimator>();

            if (board != null && hud != null)
                yield return RunInParallelCoroutines(
                    board.PlayRollAnimation(result, OnFace),
                    hud.PlayRoll(result, null));
            else if (board != null)
                yield return board.PlayRollAnimation(result, OnFace);
            else if (hud != null)
                yield return hud.PlayRoll(result, OnFace);
            else
                yield return FallbackRollAnimation(result, OnFace);

            Audio.GameAudioController.Instance?.PlayDiceLand();
            BoardCameraController.ShakeInstance(0.08f, 0.15f);

            if (hud != null)
                yield return hud.PlayResultPunch(result);
            else
                OnRollComplete?.Invoke(result);

            GameManager.Instance?.OnDiceResult(result);
            IsRolling = false;
        }

        private static IEnumerator FallbackRollAnimation(int result, Action<int> onFace)
        {
            for (int i = 0; i < DiceJuice.RollTicks; i++)
            {
                int display = UnityEngine.Random.Range(GameConfig.MinDice, GameConfig.MaxDice + 1);
                onFace?.Invoke(display);
                yield return new WaitForSeconds(GameConfig.AnimationDuration(DiceJuice.TickInterval));
            }
            onFace?.Invoke(result);
        }

        public void RegisterHudAnimator(DiceHudAnimator animator) => _hudAnimator = animator;

        private IEnumerator RunInParallelCoroutines(IEnumerator first, IEnumerator second)
        {
            if (first == null) { if (second != null) yield return second; yield break; }
            if (second == null) { yield return first; yield break; }

            bool firstDone = false;
            bool secondDone = false;
            StartCoroutine(RunAndMark(first, () => firstDone = true));
            StartCoroutine(RunAndMark(second, () => secondDone = true));
            yield return new WaitUntil(() => firstDone && secondDone);
        }

        private static IEnumerator RunAndMark(IEnumerator routine, Action onDone)
        {
            if (routine != null) yield return routine;
            onDone?.Invoke();
        }
    }
}
