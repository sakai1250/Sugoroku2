using System.Collections;
using System.Collections.Generic;
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
            player.LastDiceValue = -1;
            OnTurnStarted?.Invoke(player);

            yield return StartCoroutine(ResolvePendingResults(player));

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

            if (player.LastDiceValue == steps)
            {
                player.HasExtraRoll = true;
                BoardCameraController.ShakeInstance(0.14f, 0.22f);
                Sugoroku.UI.GameStatusBanner.Show(
                    $"{PlayerIdentity.FormatHudLabel(player)} — ゾロ目！（{steps}）もう1回振れます！");
            }
            player.LastDiceValue = steps;

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

            if (PlayerInteractionRules.IsCollabEligible(squareType) &&
                PlayerInteractionRules.TryFindCoOccupant(player, GameManager.Instance.GetAllPlayers(), out var coOccupant))
            {
                yield return StartCoroutine(ResolveCollabBonus(player, coOccupant));
            }
            else if (PlayerInteractionRules.IsEquipmentContestEligible(squareType) &&
                     PlayerInteractionRules.TryFindCoOccupant(player, GameManager.Instance.GetAllPlayers(), out coOccupant))
            {
                yield return StartCoroutine(ResolveEquipmentContest(player, coOccupant));
            }

            if (player.IgnoreNextEvents > 0 && IsEventType(squareType))
            {
                player.IgnoreNextEvents--;
                GameManager.Instance.ShowSquareEffect(player, squareType, ignored: true);
                yield return new WaitForSeconds(1f);
                EndTurn();
                yield break;
            }

            bool isChoiceSquare = squareType == SquareType.Event || squareType == SquareType.Branch;
            SetState(isChoiceSquare ? TurnState.Event : TurnState.Apply);
            yield return new WaitForSeconds(0.5f);

            switch (squareType)
            {
                case SquareType.Event:
                    if (BranchRouteRules.IsInBranchRange(player.BoardPosition) && player.ActiveBranch != BranchRoute.None)
                    {
                        yield return StartCoroutine(ResolveBranchSquareEffect(player));
                        break;
                    }
                    var ev = node?.ResolveBoundEvent() ?? EventManager.Instance.DrawEvent();
                    if (ev != null)
                        EventManager.Instance.TriggerEvent(ev, player);
                    else
                        EndTurn();
                    break;

                case SquareType.Branch:
                    var forkEv = node?.ResolveBoundEvent() ?? EventManager.Instance.GetById(BranchRouteRules.ForkEventId);
                    if (forkEv != null)
                        EventManager.Instance.TriggerEvent(forkEv, player);
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
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    if (!isSeriousImmune)
                        yield return StatChangeSequencer.Apply(player, 0, -5, -10, 5);
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
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    yield return StatChangeSequencer.Apply(player,
                        node?.BonusMoney ?? 5, 5, 10, 5);
                    EndTurn();
                    break;

                case SquareType.Penalty:
                    GameManager.Instance.ShowSquareEffect(player, squareType);
                    yield return StatChangeSequencer.Apply(player,
                        node?.PenaltyMoney ?? -5, -5, -10, -5);
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
            var player = CurrentPlayer;
            OnTurnEnded?.Invoke(player);

            if (player != null && player.HasExtraRoll && !player.IsFinished)
            {
                player.HasExtraRoll = false;
                StartCoroutine(StartExtraRoll(player));
                return;
            }

            GameManager.Instance.AdvanceTurn();
        }

        private IEnumerator StartExtraRoll(PlayerData player)
        {
            yield return new WaitForSeconds(GameConfig.AnimationDuration(0.35f));
            BoardDicePlacement.PlaceNearPlayer(player);
            SetState(TurnState.WaitAction);

            if (player.IsCpu)
            {
                yield return new WaitForSeconds(0.8f);
                CpuController.Instance.DecideAction(player);
            }
        }

        private bool IsEventType(SquareType t)
        {
            return t == SquareType.Event || t == SquareType.Tuition ||
                   t == SquareType.Journal || t == SquareType.Lecture ||
                   t == SquareType.Branch;
        }

        /// <summary>「査読中…」システム: 自分のターン開始時に結果待ちを1減算し、届いたものを解決する。</summary>
        private IEnumerator ResolvePendingResults(PlayerData player)
        {
            if (player.PendingResults.Count == 0) yield break;

            var due = new List<PendingEventResult>();
            for (int i = player.PendingResults.Count - 1; i >= 0; i--)
            {
                var pending = player.PendingResults[i];
                pending.TurnsRemaining--;
                if (pending.TurnsRemaining <= 0)
                {
                    due.Add(pending);
                    player.PendingResults.RemoveAt(i);
                }
            }

            foreach (var pending in due)
            {
                bool accepted = GameRng.Range(0, 99) < pending.AcceptProbabilityPercent;
                string text = accepted ? pending.AcceptedText : pending.RejectedText;
                if (!string.IsNullOrEmpty(text))
                    Sugoroku.UI.GameStatusBanner.Show($"{PlayerIdentity.FormatHudLabel(player)} — {text}");

                yield return new WaitForSeconds(GameConfig.AnimationDuration(0.8f));

                if (accepted)
                    yield return StatChangeSequencer.Apply(player,
                        pending.AcceptMoneyChange, pending.AcceptIfScoreChange,
                        pending.AcceptMentalChange, pending.AcceptVirtueChange);
                else
                    yield return StatChangeSequencer.Apply(player,
                        pending.RejectMoneyChange, pending.RejectIfScoreChange,
                        pending.RejectMentalChange, pending.RejectVirtueChange);
            }
        }

        /// <summary>同じマスに他プレイヤーが滞在していた場合の「共同研究」ボーナス。両者に小さくIF/徳を加算。</summary>
        private IEnumerator ResolveCollabBonus(PlayerData player, PlayerData other)
        {
            Sugoroku.UI.GameStatusBanner.Show(
                $"★ 共同研究！ {PlayerIdentity.FormatHudLabel(player)} と {PlayerIdentity.FormatHudLabel(other)} が鉢合わせ");

            yield return StatChangeSequencer.Apply(player, 0, 5, 0, 2);
            yield return StatChangeSequencer.Apply(other, 0, 5, 0, 2);
        }

        /// <summary>同じマスで装置や席を取り合う小さな対人イベント。移動者が機材を確保し、先客は少しメンタルを削られる。</summary>
        private IEnumerator ResolveEquipmentContest(PlayerData player, PlayerData other)
        {
            Sugoroku.UI.GameStatusBanner.Show(
                $"★ 機材の取り合い！ {PlayerIdentity.FormatHudLabel(player)} が装置を確保、{PlayerIdentity.FormatHudLabel(other)} は待機");

            yield return StatChangeSequencer.Apply(player, 0, 3, 0, 0);
            int mentalPenalty = other.Mental > 1 ? -Mathf.Min(5, other.Mental - 1) : 0;
            if (mentalPenalty != 0)
                yield return StatChangeSequencer.Apply(other, 0, 0, mentalPenalty, 0);
        }

        /// <summary>分岐ルート区間のマス効果。研究室=IF重視・メンタル消耗、バイト=金重視・IF停滞。</summary>
        private IEnumerator ResolveBranchSquareEffect(PlayerData player)
        {
            bool isLab = player.ActiveBranch == BranchRoute.Lab;
            GameManager.Instance.ShowSquareEffect(player, isLab ? SquareType.Journal : SquareType.PartTime);
            Sugoroku.UI.GameStatusBanner.Show(isLab
                ? $"{PlayerIdentity.FormatHudLabel(player)} — 研究室ルート: 実験に没頭"
                : $"{PlayerIdentity.FormatHudLabel(player)} — バイトルート: シフトに入る");

            if (isLab)
                yield return StatChangeSequencer.Apply(player, 0, 8, -6, 0);
            else
                yield return StatChangeSequencer.Apply(player, 9, 0, 2, 0);

            if (player.BoardPosition >= BranchRouteRules.RangeEnd)
                player.ActiveBranch = BranchRoute.None;

            EndTurn();
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
            int boardIndex = player.BoardPosition;
            if (GameManager.Instance.IsJournalClaimed(boardIndex))
            {
                ifGain /= 2;
                Sugoroku.UI.GameStatusBanner.Show(
                    $"{PlayerIdentity.FormatHudLabel(player)} — 先を越された…！IF半減");
            }
            else
            {
                GameManager.Instance.ClaimJournal(boardIndex);
            }

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
