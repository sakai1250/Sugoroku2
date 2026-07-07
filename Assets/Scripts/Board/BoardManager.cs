using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.Board
{
    [DefaultExecutionOrder(-100)]
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [SerializeField] private WaypointRoute _route;
        [SerializeField] private LayeredBoardGenerator _generator;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_route == null)
                _route = GetComponentInChildren<WaypointRoute>(true);

            if (_generator == null)
                _generator = GetComponent<LayeredBoardGenerator>();
        }

        public WaypointRoute Route => _route;

        /// <summary>論理マス数（ゲーム進行用）。</summary>
        public int BoardSize => BoardNavigation.LogicalCellCount;

        public void SetRoute(WaypointRoute route)
        {
            _route = route;
        }

        public int ToPhysical(PlayerData player) =>
            player == null ? 0 : BoardNavigation.ToPhysical(player.BoardPosition, player.ActiveBranch);

        public int ToPhysical(int logicalIndex, PlayerData player) =>
            player == null ? logicalIndex : BoardNavigation.ToPhysical(logicalIndex, player.ActiveBranch);

        public Waypoint GetWaypoint(int physicalIndex) =>
            _route != null ? _route.GetWaypoint(physicalIndex) : null;

        public Waypoint GetWaypoint(PlayerData player) =>
            _route != null && player != null ? _route.GetWaypoint(ToPhysical(player)) : null;

        public Vector3 GetPosition(int physicalIndex) =>
            _route != null ? _route.GetPosition(physicalIndex) : Vector3.zero;

        public Vector3 GetPosition(PlayerData player) =>
            _route != null && player != null ? _route.GetPosition(ToPhysical(player)) : Vector3.zero;

        public Vector3 GetPositionAtLogical(int logicalIndex, PlayerData player) =>
            _route != null && player != null
                ? _route.GetPosition(BoardNavigation.ToPhysical(logicalIndex, player.ActiveBranch))
                : Vector3.zero;

        public SquareType GetSquareType(int logicalIndex) =>
            BoardNavigation.GetSquareType(logicalIndex, BranchRoute.None);

        public SquareType GetSquareType(PlayerData player) =>
            player != null
                ? BoardNavigation.GetSquareType(player.BoardPosition, player.ActiveBranch)
                : SquareType.Normal;

        public int GetNextTuitionIndex(int currentLogicalIndex) =>
            GetNextTuitionIndex(currentLogicalIndex, BranchRoute.None);

        public int GetNextTuitionIndex(int currentLogicalIndex, BranchRoute branch)
        {
            for (int i = currentLogicalIndex + 1; i < BoardNavigation.LogicalCellCount; i++)
                if (BoardNavigation.GetSquareType(i, branch) == SquareType.Tuition)
                    return i - currentLogicalIndex;
            return BoardNavigation.LogicalCellCount - currentLogicalIndex;
        }

        public int GetNextTuitionIndex(PlayerData player) =>
            player == null ? 0 : GetNextTuitionIndex(player.BoardPosition, player.ActiveBranch);

        public void GenerateDefaultBoard()
        {
            if (_route != null && _route.Count > 0) return;

            if (_generator != null)
            {
                _generator.GenerateLayeredBoard();
                return;
            }

            Debug.LogWarning("BoardManager: WaypointRoute が空です。LayeredBoardGenerator を Board に追加してください。");
        }
    }
}
