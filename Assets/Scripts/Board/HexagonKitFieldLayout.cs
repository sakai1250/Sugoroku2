using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>
    /// Kenney Hexagon Kit 用の axial (q,r) → ワールド座標変換（フラットトップ六角）。
    /// </summary>
    public static class HexagonKitFieldLayout
    {
        public static Vector3 AxialToWorld(int q, int r, float tileWorldWidth)
        {
            float w = Mathf.Max(0.5f, tileWorldWidth);
            float x = w * (1.5f * q);
            float y = w * (Mathf.Sqrt(3f) * (r + q * 0.5f));
            return new Vector3(x, y, 0f);
        }

        public static Vector2Int[] CollectPathCorridor(Vector2Int[] path, int rings)
        {
            if (path == null || path.Length == 0) return System.Array.Empty<Vector2Int>();
            rings = Mathf.Max(0, rings);

            var set = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var cell in path)
            {
                set.Add(cell);
                for (int dq = -rings; dq <= rings; dq++)
                for (int dr = -rings; dr <= rings; dr++)
                {
                    if (Mathf.Abs(dq + dr) > rings) continue;
                    set.Add(new Vector2Int(cell.x + dq, cell.y + dr));
                }
            }

            var list = new Vector2Int[set.Count];
            set.CopyTo(list);
            return list;
        }
    }
}
