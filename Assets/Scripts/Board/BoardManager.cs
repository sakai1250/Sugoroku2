using UnityEngine;
using Sugoroku.Data;

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

        public int BoardSize =>
            _route != null && _route.Count > 0
                ? _route.Count
                : SnakeBoardLayout.CellCount;

        public void SetRoute(WaypointRoute route)
        {
            _route = route;
        }

        public Waypoint GetWaypoint(int index) =>
            _route != null ? _route.GetWaypoint(index) : null;

        public Vector3 GetPosition(int index) =>
            _route != null ? _route.GetPosition(index) : Vector3.zero;

        public SquareType GetSquareType(int index) =>
            _route != null ? _route.GetSquareType(index) : SquareType.Normal;

        public int GetNextTuitionIndex(int currentIndex)
        {
            for (int i = currentIndex + 1; i < BoardSize; i++)
                if (GetSquareType(i) == SquareType.Tuition)
                    return i - currentIndex;
            return BoardSize - currentIndex;
        }

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
