using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>論理マス位置と物理ウェイポイント（分岐レーン）の対応。</summary>
    public static class BoardNavigation
    {
        public const float BranchLaneOffsetRatio = 1.4f;
        const float BranchLaneMargin = 0.55f;

        /// <summary>レーン中心線の本線からのオフセット（カード高さを考慮）。</summary>
        public static float ComputeLaneOffset(float spacingY)
        {
            float cardHalf = MassTextCardPrefabFactory.CardWorldHeight * 0.5f + BranchLaneMargin;
            return Mathf.Max(spacingY * BranchLaneOffsetRatio, cardHalf * 2.2f);
        }

        public static Vector3 GetBranchLanePerpendicular(Vector3 forkPos, Vector3 mergePos)
        {
            var along = mergePos - forkPos;
            if (along.sqrMagnitude < 0.001f)
                return Vector3.up;
            var alongNorm = along.normalized;
            return new Vector3(-alongNorm.y, alongNorm.x, 0f);
        }

        public static int LogicalCellCount => BoardLayoutGenerator.Current.CellCount;

        public static int BranchLength
        {
            get
            {
                var d = BoardLayoutGenerator.Current;
                return d.BranchRangeEnd - d.BranchRangeStart + 1;
            }
        }

        public static int MergeLogicalIndex => BoardLayoutGenerator.Current.BranchRangeEnd + 1;

        public static int PhysicalWaypointCount
        {
            get
            {
                var d = BoardLayoutGenerator.Current;
                int tail = d.CellCount - MergeLogicalIndex;
                return d.ForkIndex + 1 + BranchLength * 2 + tail;
            }
        }

        public static int LabPhysicalStart => BoardLayoutGenerator.Current.ForkIndex + 1;

        public static int PartTimePhysicalStart => LabPhysicalStart + BranchLength;

        public static int TailPhysicalStart => PartTimePhysicalStart + BranchLength;

        public static BranchRoute GetLane(int physicalIndex)
        {
            if (physicalIndex < LabPhysicalStart) return BranchRoute.None;
            if (physicalIndex < PartTimePhysicalStart) return BranchRoute.Lab;
            if (physicalIndex < TailPhysicalStart) return BranchRoute.PartTime;
            return BranchRoute.None;
        }

        public static int GetLogical(int physicalIndex)
        {
            var d = BoardLayoutGenerator.Current;
            if (physicalIndex <= d.ForkIndex) return physicalIndex;
            if (physicalIndex < TailPhysicalStart)
            {
                int step = physicalIndex - LabPhysicalStart;
                if (physicalIndex >= PartTimePhysicalStart)
                    step = physicalIndex - PartTimePhysicalStart;
                return d.BranchRangeStart + step;
            }

            return MergeLogicalIndex + (physicalIndex - TailPhysicalStart);
        }

        public static int ToPhysical(int logical, BranchRoute branch)
        {
            var d = BoardLayoutGenerator.Current;
            if (logical <= d.ForkIndex) return logical;

            if (logical > d.BranchRangeEnd)
                return TailPhysicalStart + (logical - MergeLogicalIndex);

            int step = logical - d.BranchRangeStart;
            if (branch == BranchRoute.PartTime)
                return PartTimePhysicalStart + step;
            return LabPhysicalStart + step;
        }

        public static Vector3 GetWorldPosition(int logical, BranchRoute branch, float spacingX, float spacingY)
        {
            if (!BranchRouteRules.IsInBranchRange(logical) || branch == BranchRoute.None)
                return SnakeBoardLayout.GetGridWorldPosition(logical, spacingX, spacingY);

            var d = BoardLayoutGenerator.Current;
            var forkPos  = SnakeBoardLayout.GetGridWorldPosition(d.ForkIndex, spacingX, spacingY);
            var mergePos = SnakeBoardLayout.GetGridWorldPosition(MergeLogicalIndex, spacingX, spacingY);
            int step = logical - d.BranchRangeStart;
            int len  = BranchLength;

            // フォークと合流点の間にレーンを均等配置（本線グリッドと重ならない）
            float t = (step + 1f) / (len + 1f);
            var trackCenter = Vector3.Lerp(forkPos, mergePos, t);
            var perp = GetBranchLanePerpendicular(forkPos, mergePos);
            float laneOffset = ComputeLaneOffset(spacingY);
            float side = branch == BranchRoute.Lab ? laneOffset : -laneOffset;
            return trackCenter + perp * side;
        }

        public static SquareType GetSquareType(int logical, BranchRoute lane)
        {
            if (!BranchRouteRules.IsInBranchRange(logical) || lane == BranchRoute.None)
                return SnakeBoardLayout.GetSquareType(logical);

            int step = logical - BoardLayoutGenerator.Current.BranchRangeStart;
            if (lane == BranchRoute.Lab)
            {
                return step switch
                {
                    0 => SquareType.Journal,
                    1 => SquareType.Event,
                    _ => SquareType.Lecture
                };
            }

            return step switch
            {
                0 => SquareType.PartTime,
                1 => SquareType.Event,
                _ => SquareType.Tuition
            };
        }

        public static string GetDisplayName(int logical, BranchRoute lane)
        {
            if (logical == BranchRouteRules.ForkIndex) return "進路の分岐点";

            if (BranchRouteRules.IsInBranchRange(logical) && lane != BranchRoute.None)
            {
                int step = logical - BoardLayoutGenerator.Current.BranchRangeStart;
                if (lane == BranchRoute.Lab)
                {
                    return step switch
                    {
                        0 => "研究室ルート",
                        1 => "実験に没頭",
                        _ => "査読地獄"
                    };
                }

                return step switch
                {
                    0 => "バイトルート",
                    1 => "シフト入り",
                    _ => "生活費確保"
                };
            }

            return SnakeBoardLayout.GetDisplayName(logical);
        }

        public static string GetDisplayLabel(int logical, BranchRoute lane)
        {
            var named = GetDisplayName(logical, lane);
            if (!string.IsNullOrEmpty(named)) return named;
            var type = GetSquareType(logical, lane);
            var shortTag = Waypoint.GetTypeShortLabel(type);
            return string.IsNullOrEmpty(shortTag) ? $"マス{logical + 1}" : $"{shortTag}・マス{logical + 1}";
        }

        public static string GetEventId(int logical, BranchRoute lane)
        {
            if (BranchRouteRules.IsInBranchRange(logical) && lane != BranchRoute.None)
                return BoardLayoutGenerator.GetBranchEventId(logical, lane);
            return SnakeBoardLayout.GetEventId(logical);
        }

        /// <summary>盤面上に描画する接続線（両レーンを常に表示）。</summary>
        public static void CollectConnectorSegments(WaypointRoute route, System.Collections.Generic.List<(Vector3 a, Vector3 b)> segments)
        {
            if (route == null || route.Count < 2) return;
            segments.Clear();

            var d = BoardLayoutGenerator.Current;
            int fork = d.ForkIndex;
            int len = BranchLength;

            AppendChain(segments, route, 0, fork);
            if (len <= 0) return;

            int labStart = LabPhysicalStart;
            int ptStart = PartTimePhysicalStart;
            int mergePhysical = TailPhysicalStart;

            segments.Add((route.GetPosition(fork), route.GetPosition(labStart)));
            AppendChain(segments, route, labStart, labStart + len - 1);
            segments.Add((route.GetPosition(labStart + len - 1), route.GetPosition(mergePhysical)));

            segments.Add((route.GetPosition(fork), route.GetPosition(ptStart)));
            AppendChain(segments, route, ptStart, ptStart + len - 1);
            segments.Add((route.GetPosition(ptStart + len - 1), route.GetPosition(mergePhysical)));

            AppendChain(segments, route, mergePhysical, route.Count - 1);
        }

        static void AppendChain(System.Collections.Generic.List<(Vector3 a, Vector3 b)> segments,
            WaypointRoute route, int from, int to)
        {
            for (int i = from; i < to; i++)
                segments.Add((route.GetPosition(i), route.GetPosition(i + 1)));
        }
    }
}
