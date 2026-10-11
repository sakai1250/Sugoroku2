using System;
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
        public SeasonType Season = SeasonType.ResearchWriting;
    }

    /// <summary>マス数およびシーズンに応じた S字盤面データを生成する。</summary>
    public static class BoardLayoutGenerator
    {
        static BoardLayoutData _cached;
        static int _cachedCellCount = -1;
        static int _cachedSeed;
        static SeasonType _cachedSeason = (SeasonType)(-1);

        static readonly string[] DefaultEventPool =
        {
            "E012", "E013", "E014", "E015", "E016",
            "E017", "E018", "E020", "E021", "E022",
            "E023", "E024", "E025", "E026", "E027",
            "E028", "E029", "E030", "E031", "E032"
        };

        static readonly string[] ResearchEventPool =
        {
            "E012", "E013", "E015", "E016", "E023", "E024", "E025", "E026", "E027"
        };

        static readonly string[] ConferenceEventPool =
        {
            "E014", "E017", "E018", "E020", "E028", "E029", "E030"
        };

        static readonly string[] PartTimeEventPool =
        {
            "E019", "E021", "E022", "E031", "E032"
        };

        public static void Invalidate()
        {
            _cached = null;
            _cachedCellCount = -1;
            _cachedSeason = (SeasonType)(-1);
        }

        public static BoardLayoutData Current
        {
            get
            {
                int count = GameSession.BoardCellCount;
                int seed  = GameSession.IsDailyChallenge ? GameSession.DailySeed : 0;
                var season = GameSession.CurrentSeason;
                if (_cached == null || _cachedCellCount != count || _cachedSeed != seed || _cachedSeason != season)
                {
                    _cached = Build(count, seed, season);
                    _cachedCellCount = count;
                    _cachedSeed = seed;
                    _cachedSeason = season;
                }
                return _cached;
            }
        }

        public static BoardLayoutData Build(int cellCount, int seed = 0)
        {
            return Build(cellCount, seed, GameSession.CurrentSeason);
        }

        public static BoardLayoutData Build(int cellCount, int seed, SeasonType season)
        {
            cellCount = Mathf.Clamp(cellCount, 12, 40);
            var data = new BoardLayoutData
            {
                CellCount = cellCount,
                Columns   = 5,
                Rows      = Mathf.CeilToInt(cellCount / 5f),
                SquareTypes = new SquareType[cellCount],
                EventIds    = new string[cellCount],
                DisplayNames = new string[cellCount],
                Season = season
            };

            int fork = ComputeForkIndex(cellCount);
            data.ForkIndex = fork;
            data.BranchRangeStart = fork + 1;
            data.BranchRangeEnd   = Mathf.Min(fork + 3, cellCount - 2);

            var rng = seed != 0 ? new System.Random(seed) : null;
            string[] eventPool = season switch
            {
                SeasonType.ResearchWriting => ResearchEventPool,
                SeasonType.Conference      => ConferenceEventPool,
                SeasonType.PartTime        => PartTimeEventPool,
                _                          => DefaultEventPool
            };
            if (eventPool == null || eventPool.Length == 0) eventPool = DefaultEventPool;

            int eventCursor = rng != null ? rng.Next(0, eventPool.Length) : 0;

            for (int i = 0; i < cellCount; i++)
            {
                if (i == 0)
                {
                    data.SquareTypes[i] = SquareType.Start;
                    data.DisplayNames[i] = GetStartTitle(season);
                    data.EventIds[i] = "";
                    continue;
                }

                if (i == cellCount - 1)
                {
                    data.SquareTypes[i] = SquareType.Goal;
                    data.DisplayNames[i] = GetGoalTitle(season);
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

                data.SquareTypes[i] = PickSquareType(i, cellCount, season);
                data.DisplayNames[i] = PickDisplayName(i, cellCount, fork, season);
                if (data.SquareTypes[i] == SquareType.Event)
                {
                    data.EventIds[i] = eventPool[eventCursor % eventPool.Length];
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

        static string GetStartTitle(SeasonType season) => season switch
        {
            SeasonType.ResearchWriting => "研究室配属",
            SeasonType.Conference      => "学会会場到着",
            SeasonType.PartTime        => "バイト生活突入",
            _                          => "スタート"
        };

        static string GetGoalTitle(SeasonType season) => season switch
        {
            SeasonType.ResearchWriting => "論文ドラフト提出",
            SeasonType.Conference      => "学会総括・成果報告",
            SeasonType.PartTime        => "学位審査・修了判定",
            _                          => "修了判定"
        };

        static SquareType PickSquareType(int index, int cellCount, SeasonType season)
        {
            if (index == cellCount - 2)
                return SquareType.Journal;

            return season switch
            {
                SeasonType.ResearchWriting => PickResearchSquareType(index, cellCount),
                SeasonType.Conference      => PickConferenceSquareType(index, cellCount),
                SeasonType.PartTime        => PickPartTimeSquareType(index, cellCount),
                _                          => PickDefaultSquareType(index, cellCount)
            };
        }

        static SquareType PickResearchSquareType(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return SquareType.Lecture;
            if (IsMilestone(index, cellCount, 0.30f)) return SquareType.Journal;
            if (IsMilestone(index, cellCount, 0.50f)) return SquareType.Tuition;
            if (IsMilestone(index, cellCount, 0.70f)) return SquareType.Journal;
            if (IsMilestone(index, cellCount, 0.85f)) return SquareType.Rest;
            return SquareType.Event;
        }

        static SquareType PickConferenceSquareType(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return SquareType.Tuition;
            if (IsMilestone(index, cellCount, 0.30f)) return SquareType.Journal;
            if (IsMilestone(index, cellCount, 0.50f)) return SquareType.Tuition;
            if (IsMilestone(index, cellCount, 0.70f)) return SquareType.Bonus;
            if (IsMilestone(index, cellCount, 0.85f)) return SquareType.Journal;
            return SquareType.Event;
        }

        static SquareType PickPartTimeSquareType(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return SquareType.PartTime;
            if (IsMilestone(index, cellCount, 0.35f)) return SquareType.PartTime;
            if (IsMilestone(index, cellCount, 0.55f)) return SquareType.Rest;
            if (IsMilestone(index, cellCount, 0.75f)) return SquareType.PartTime;
            if (IsMilestone(index, cellCount, 0.88f)) return SquareType.Tuition;
            return SquareType.Event;
        }

        static SquareType PickDefaultSquareType(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.20f) || IsMilestone(index, cellCount, 0.55f))
                return SquareType.Tuition;
            if (IsMilestone(index, cellCount, 0.35f) || IsMilestone(index, cellCount, 0.72f))
                return SquareType.Journal;
            if (IsMilestone(index, cellCount, 0.12f))
                return SquareType.Lecture;
            if (IsMilestone(index, cellCount, 0.48f) || IsMilestone(index, cellCount, 0.85f))
                return SquareType.PartTime;
            return SquareType.Event;
        }

        static bool IsMilestone(int index, int cellCount, float ratio)
        {
            int target = Mathf.Clamp(Mathf.RoundToInt((cellCount - 1) * ratio), 1, cellCount - 2);
            return index == target;
        }

        static string PickDisplayName(int index, int cellCount, int fork, SeasonType season)
        {
            if (index == fork) return "進路の分岐点";

            if (index == cellCount - 2)
            {
                return season switch
                {
                    SeasonType.ResearchWriting => "ドラフト推敲",
                    SeasonType.Conference      => "国際交流",
                    SeasonType.PartTime        => "修論最終提出",
                    _                          => "修論提出"
                };
            }

            return season switch
            {
                SeasonType.ResearchWriting => PickResearchDisplayName(index, cellCount),
                SeasonType.Conference      => PickConferenceDisplayName(index, cellCount),
                SeasonType.PartTime        => PickPartTimeDisplayName(index, cellCount),
                _                          => PickDefaultDisplayName(index, cellCount)
            };
        }

        static string PickResearchDisplayName(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return "ゼミ発表";
            if (IsMilestone(index, cellCount, 0.30f)) return "先行研究調査";
            if (IsMilestone(index, cellCount, 0.50f)) return "学費納入";
            if (IsMilestone(index, cellCount, 0.70f)) return "論文執筆";
            if (IsMilestone(index, cellCount, 0.85f)) return "息抜き";
            return "";
        }

        static string PickConferenceDisplayName(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return "渡航費・宿泊費";
            if (IsMilestone(index, cellCount, 0.30f)) return "ポスター発表";
            if (IsMilestone(index, cellCount, 0.50f)) return "学会参加費";
            if (IsMilestone(index, cellCount, 0.70f)) return "若手奨励賞";
            if (IsMilestone(index, cellCount, 0.85f)) return "口頭発表";
            return "";
        }

        static string PickPartTimeDisplayName(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.15f)) return "TA業務";
            if (IsMilestone(index, cellCount, 0.35f)) return "深夜バイト";
            if (IsMilestone(index, cellCount, 0.55f)) return "サウナ・休養";
            if (IsMilestone(index, cellCount, 0.75f)) return "塾講師バイト";
            if (IsMilestone(index, cellCount, 0.88f)) return "学費納入";
            return "";
        }

        static string PickDefaultDisplayName(int index, int cellCount)
        {
            if (IsMilestone(index, cellCount, 0.10f)) return "ゼミ発表";
            if (IsMilestone(index, cellCount, 0.48f)) return "バイト";
            if (IsMilestone(index, cellCount, 0.20f) || IsMilestone(index, cellCount, 0.55f)) return "学費納入";
            if (IsMilestone(index, cellCount, 0.35f) || IsMilestone(index, cellCount, 0.72f)) return "ジャーナル";
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
