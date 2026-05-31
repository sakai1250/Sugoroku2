using System.Collections;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Board;
using Sugoroku.Network;

namespace Sugoroku.Game
{
    public enum TurnState
    {
        TurnStart,
        WaitAction,
        Moving,
        MassCheck,
        Event,
        Apply,
        TurnEnd
    }

    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public TurnState  CurrentState { get; private set; }
        public PlayerData CurrentPlayer => GameManager.Instance.GetCurrentPlayer();

        public event System.Action<TurnState>    OnStateChanged;
        public event System.Action<PlayerData>   OnTurnStarted;
        public event System.Action<PlayerData>   OnTurnEnded;
        public event System.Action<PlayerData, int> OnPlayerMoved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartTurn()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsInitialized) return;
            SetState(TurnState.TurnStart);
            StartCoroutine(TurnStartCoroutine());
        }

        private IEnumerator TurnStartCoroutine()
        {
            var player = CurrentPlayer;
            OnTurnStarted?.Invoke(player);

            if (player.SkipTurns > 0)
            {
                player.SkipTurns--;
                yield return new WaitForSeconds(1f);
                EndTurn();
                yield break;
            }

            player.SkillUsedThisTurn = false;
            player.AthleticMentalImmunity = false;
            BoardDicePlacement.PlaceNearPlayer(player);
            SetState(TurnState.WaitAction);

            if (player.IsCpu)
            {
                yield return new WaitForSeconds(0.8f);
                CpuController.Instance.DecideAction(player);
            }
        }

        public void OnDiceRolled(int steps)
        {
            var player = CurrentPlayer;
            BoardDicePlacement.PlaceNearPlayer(player);
            BoardCameraController.Instance?.PreviewDiceRoll(steps, player);
            SetState(TurnState.Moving);
            StartCoroutine(MoveCoroutine(player, steps));
        }

        private IEnumerator MoveCoroutine(PlayerData player, int steps)
        {
            int board = BoardManager.Instance.BoardSize;
            for (int i = 0; i < steps; i++)
            {
                player.BoardPosition++;
                if (player.BoardPosition >= board)
                {
                    player.BoardPosition = board - 1;
                    GameManager.Instance.OnPlayerReachGoal(player);
                    yield break;
                }

                Vector3 target = BoardManager.Instance.GetPosition(player.BoardPosition);
                yield return StartCoroutine(MovePiece(player, target));
            }

            OnPlayerMoved?.Invoke(player, player.BoardPosition);
            SetState(TurnState.MassCheck);
            yield return StartCoroutine(MassCheckCoroutine(player));
        }

        private IEnumerator MovePiece(PlayerData player, Vector3 target)
        {
            var piece = GameManager.Instance.GetPiece(player.Index);
            if (piece == null) { yield break; }

            float elapsed = 0f;
            Vector3 start = piece.transform.position;
            Vector3 baseScale = piece.transform.localScale;
            float dur = GameConfig.AnimationDuration(GameConfig.PieceMoveDuration);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float arc = Mathf.Sin(eased * Mathf.PI) * 0.5f;
                float stretch = Mathf.Sin(eased * Mathf.PI);
                piece.transform.position = Vector3.Lerp(start, target, eased) + Vector3.up * arc;
                piece.transform.localScale = new Vector3(
                    baseScale.x * (1f - 0.08f * stretch),
                    baseScale.y * (1f + 0.10f * stretch),
                    baseScale.z);
                BoardCameraController.Instance?.FollowPosition(piece.transform.position);
                yield return null;
            }
            piece.transform.position = target;
            piece.transform.localScale = new Vector3(baseScale.x * 1.08f, baseScale.y * 0.90f, baseScale.z);
            BoardEffectPresenter.Instance?.PlayStepLanding(player, target);
            yield return new WaitForSeconds(GameConfig.AnimationDuration(0.04f));
            piece.transform.localScale = baseScale;
        }

        private IEnumerator MassCheckCoroutine(PlayerData player)
        {
            var squareType = BoardManager.Instance.GetSquareType(player.BoardPosition);
            var node       = BoardManager.Instance.GetWaypoint(player.BoardPosition);

            if (player.IgnoreNextEvents > 0 && IsEventType(squareType))
            {
                player.IgnoreNextEvents--;
                GameManager.Instance.ShowSquareEffect(player, squareType, ignored: true);
                yield return new WaitForSeconds(1f);
                EndTurn();
                yield break;
            }

            SetState(squareType == SquareType.Event ? TurnState.Event : TurnState.Apply);
            yield return new WaitForSeconds(0.5f);

            switch (squareType)
            {
                case SquareType.Event:
                    var ev = node?.ResolveBoundEvent() ?? EventManager.Instance.DrawEvent();
                    if (ev != null)
                        EventManager.Instance.TriggerEvent(ev, player);
                    else
                        EndTurn();
                    break;

                case SquareType.Tuition:
                    int cost = node?.TuitionAmount ?? GameConfig.TuitionCost;
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    player.ApplyStatChange(-cost, 0, 0, 0);
                    EndTurn();
                    break;

                case SquareType.Journal:
                    ResolveJournalSquare(player, node?.JournalIfGain ?? 10, squareType);
                    break;

                case SquareType.Lecture:
                    bool isSeriousImmune = player.Character == CharacterType.Serious;
                    if (!isSeriousImmune)
                        player.ApplyStatChange(0, -5, -10, 5);
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;

                case SquareType.Rest:
                    int mentalGain = node?.RestMentalGain ?? 20;
                    player.ApplyStatChange(0, 0, mentalGain, 0);
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;

                case SquareType.PartTime:
                    player.ApplyStatChange(node?.BonusMoney ?? 8, 0, 0, 0);
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;

                case SquareType.Bonus:
                    player.ApplyStatChange(node?.BonusMoney ?? 5, 5, 10, 5);
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;

                case SquareType.Penalty:
                    player.ApplyStatChange(node?.PenaltyMoney ?? -5, -5, -10, -5);
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;

                default:
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    EndTurn();
                    break;
            }
        }

        public void EndTurn()
        {
            if (GameManager.Instance == null) return;
            SetState(TurnState.TurnEnd);
            OnTurnEnded?.Invoke(CurrentPlayer);
            GameManager.Instance.AdvanceTurn();
        }

        private bool IsEventType(SquareType t)
        {
            return t == SquareType.Event || t == SquareType.Tuition ||
                   t == SquareType.Journal || t == SquareType.Lecture;
        }

        private void RequestRandomEvent(PlayerData player)
        {
            var net = NetworkSessionHost.Instance;
            EventMaster ev = net != null ? net.RequestEventDraw() : EventManager.Instance.DrawEventOnAuthority();
            if (ev != null)
                EventManager.Instance.TriggerEvent(ev, player);
            else if (net == null || !net.IsOnline)
                EndTurn();
        }

        private void ResolveJournalSquare(PlayerData player, int baseIfGain, SquareType squareType)
        {
            var net = NetworkSessionHost.Instance;
            int ifGain = net != null
                ? net.RequestJournalIfGain(player, baseIfGain)
                : GeniusJournalRules.ComputeIfGain(baseIfGain, player, GameRng.Range);

            if (ifGain >= 0)
                ApplyJournalIfGain(player, ifGain, squareType);
        }

        public void ApplyJournalIfGain(PlayerData player, int ifGain, SquareType squareType = SquareType.Journal)
        {
            player.ApplyStatChange(0, ifGain, 0, 0);
            GameManager.Instance.ShowSquareEffect(player, squareType);
            EndTurn();
        }

        private void SetState(TurnState state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
