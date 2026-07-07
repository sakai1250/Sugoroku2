using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    public sealed class BoardLayoutData
    {
        public int CellCount;
        public int Columns = 5;
        public int Rows;
        public int ForkIndex;
        public int BranchRangeStart;
        public int BranchRangeEnd;
        public SquareType[] SquareTypes;
        public string[] EventIds;
        public string[] DisplayNames;
    }

    /// <summary>マス数に応じた S字盤面データを生成する。</summary>
    public static class BoardLayoutGenerator
    {
        static BoardLayoutData _cached;
        static int _cachedCellCount = -1;
        static int _cachedSeed;

        static readonly string[] EventPool =
        {
            "E012", "E013", "E014", "E015", "E016",
            "E017", "E018", "E020", "E021", "E022",
            "E023", "E024", "E025", "E026", "E027",
            "E028", "E029", "E030", "E031", "E032"
        };

        public static void Invalidate()
        {
            _cached = null;
            _cachedCellCount = -1;
        }

        public static BoardLayoutData Current
        {
            get
            {
                int count = GameSession.BoardCellCount;
                int seed  = GameSession.IsDailyChallenge ? GameSession.DailySeed : 0;
                if (_cached == null || _cachedCellCount != count || _cachedSeed != seed)
                {
                    _cached = Build(count, seed);
                    _cachedCellCount = count;
                    _cachedSeed = seed;
                }
                return _cached;
            }
        }

        public static BoardLayoutData Build(int cellCount, int seed = 0)
        {
            cellCount = Mathf.Clamp(cellCount, 12, 28);
            var data = new BoardLayoutData
            {
                CellCount = cellCount,
                Columns   = 5,
                Rows      = Mathf.CeilToInt(cellCount / 5f),
                SquareTypes = new SquareType[cellCount],
                EventIds    = new string[cellCount],
                DisplayNames = new string[cellCount]
            };

            int fork = ComputeForkIndex(cellCount);
            data.ForkIndex = fork;
            data.BranchRangeStart = fork + 1;
            data.BranchRangeEnd   = Mathf.Min(fork + 3, cellCount - 2);

            var rng = seed != 0 ? new System.Random(seed) : null;
            int eventCursor = rng != null ? rng.Next(0, EventPool.Length) : 0;

            for (int i = 0; i < cellCount; i++)
            {
                if (i == 0)
                {
                    data.SquareTypes[i] = SquareType.Start;
                    data.DisplayNames[i] = "研究室配属";
                    data.EventIds[i] = "";
                    continue;
                }

                if (i == cellCount - 1)
                {
                    data.SquareTypes[i] = SquareType.Goal;
                    data.DisplayNames[i] = "修了判定";
                    data.EventIds[i] = "";
                    continue;
                }

                if (i == fork)
                {
                    data.SquareTypes[i] = SquareType.Branch;
                    data.DisplayNames[i] = "進路の分岐点";
                    data.EventIds[i] = BranchRouteRules.ForkEventId;
                    continue;
                }

                data.SquareTypes[i] = PickSquareType(i, cellCount);
                data.DisplayNames[i] = PickDisplayName(i, cellCount, fork);
                if (data.SquareTypes[i] == SquareType.Event)
                {
                    data.EventIds[i] = EventPool[eventCursor % EventPool.Length];
                    eventCursor++;
                }
                else
                {
                    data.EventIds[i] = "";
                }
            }

            return data;
        }

        static int ComputeForkIndex(int cellCount) =>
            Mathf.Clamp(Mathf.RoundToInt((cellCount - 2) * 0.42f), 4, cellCount - 5);

        static SquareType PickSquareType(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.20f) || IsMilestone(index, cellCount, 0.55f))
                return SquareType.Tuition;
            if (IsMilestone(index, cellCount, 0.35f) || IsMilestone(index, cellCount, 0.72f))
                return SquareType.Journal;
            if (IsMilestone(index, cellCount, 0.12f))
                return SquareType.Lecture;
            if (IsMilestone(index, cellCount, 0.48f) || IsMilestone(index, cellCount, 0.85f))
                return SquareType.PartTime;
            if (index == cellCount - 2)
                return SquareType.Journal;
            return SquareType.Event;
        }

        static bool IsMilestone(int index, int cellCount, float ratio)
        {
            int target = Mathf.Clamp(Mathf.RoundToInt((cellCount - 1) * ratio), 1, cellCount - 2);
            return index == target;
        }

        static string PickDisplayName(int index, int cellCount, int fork)
        {
            if (index == fork) return "進路の分岐点";
            if (IsMilestone(index, cellCount, 0.10f)) return "ゼミ発表";
            if (IsMilestone(index, cellCount, 0.48f)) return "バイト";
            if (IsMilestone(index, cellCount, 0.20f) || IsMilestone(index, cellCount, 0.55f)) return "学費納入";
            if (IsMilestone(index, cellCount, 0.35f) || IsMilestone(index, cellCount, 0.72f)) return "ジャーナル";
            if (index == cellCount - 2) return "修論提出";
            return "";
        }

        public static string GetBranchEventId(int logical, BranchRoute lane)
        {
            int step = logical - BoardLayoutGenerator.Current.BranchRangeStart;
            if (lane == BranchRoute.Lab)
            {
                return step switch
                {
                    0 => "E024",
                    1 => "E025",
                    _ => "E026"
                };
            }

            return step switch
            {
                0 => "E020",
                1 => "E021",
                _ => "E022"
            };
        }
    }
}
