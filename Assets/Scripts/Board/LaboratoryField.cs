using UnityEngine;
using Sugoroku.Visual;

namespace Sugoroku.Board
{
    /// <summary>
    /// 研究室タイルセット（LabTileset）で盤面周囲のフィールドを構築する。
    /// ルート外接矩形を受け取り、床タイル・上端の壁・周縁の小物（本棚/実験台/機材）を配置する。
    /// レイアウトはシード固定の擬似乱数で決定的。
    /// </summary>
    [DisallowMultipleComponent]
    public class LaboratoryField : MonoBehaviour
    {
        [SerializeField] private float _fieldMargin     = 6f;
        [SerializeField] private float _propClearance   = 1.4f;
        [SerializeField] private float _propGap         = 0.55f;
        [SerializeField] private int   _seed            = 20260821;
        [SerializeField] private bool  _enabled         = true;

        private const int FloorOrder  = -96;
        private const int WallOrder   = -90;
        private const int PropOrder   = -86;

        private static readonly string[] FloorTiles =
        {
            "floor_white", "floor_light_gray", "floor_blue",
            "floor_wood_01", "floor_wood_02", "floor_metal",
        };

        private static readonly string[] WallSegments =
        {
            "wall_plain_long", "wall_plain_long", "wall_plain_long",
            "wall_windows_long", "wall_windows_long",
            "wall_door_dark", "wall_door_wood",
        };

        // 周縁に置く小物。前半は背の低い/横長、後半は背の高いもの。
        private static readonly string[] Props =
        {
            "test_tube_rack_small", "workbench_small", "trash_bin_gray_01",
            "printer_large", "cabinet_low", "microscope_01", "microscope_02",
            "microscope_03", "lab_sink", "refrigerator", "centrifuge_large",
            "bookshelf_01", "bookshelf_02", "bookshelf_low", "lab_cabinet_tall",
            "desk_single_pc", "desk_double_pc", "whiteboard", "flasks_group",
            "fume_hood_large", "fume_hood_medium", "cabinet_dark", "cabinet_glass_wide",
        };

        private System.Random _rng;
        private readonly System.Collections.Generic.List<string> _propBag = new();

        /// <summary>同じ小物が連続で並ばないよう、シャッフルバッグから抽選する。</summary>
        private string NextProp()
        {
            if (_propBag.Count == 0)
            {
                _propBag.AddRange(Props);
                for (int i = _propBag.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (_propBag[i], _propBag[j]) = (_propBag[j], _propBag[i]);
                }
            }

            int last = _propBag.Count - 1;
            var name = _propBag[last];
            _propBag.RemoveAt(last);
            return name;
        }

        /// <summary>ルート外接矩形を基準に研究室フィールドを構築する（再呼び出し時は作り直す）。</summary>
        public void Build(Bounds routeBounds)
        {
            Clear();
            if (!_enabled) return;
            LabTileset.WarmUp();
            if (!LabTileset.IsReady) return;

            int effectiveSeed = _seed + (int)Sugoroku.Data.GameSession.CurrentSeason * 1000;
            _rng     = new System.Random(effectiveSeed);
            _propBag.Clear();
            var root = new GameObject("CampusFieldRoot").transform;
            root.SetParent(transform, false);

            var field = new Bounds(routeBounds.center, routeBounds.size + new Vector3(_fieldMargin * 2f, _fieldMargin * 2f, 0f));

            BuildFloor(root, field, _rng);
            BuildWallRow(root, field, _rng);
            BuildEdgeProps(root, field, routeBounds);
        }

        public void Clear()
        {
            var existing = transform.Find("CampusFieldRoot") ?? transform.Find("LabFieldRoot");
            if (existing == null) return;
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        // ---------------------------------------------------------------- 床

        private void BuildFloor(Transform root, Bounds field, System.Random rng)
        {
            var baseSprite = LabTileset.Get(FloorTiles[0]);
            if (baseSprite == null) return;

            float tileW = baseSprite.bounds.size.x;
            float tileH = baseSprite.bounds.size.y;
            if (tileW <= 0f || tileH <= 0f) return;

            int cols = Mathf.CeilToInt(field.size.x / tileW) + 1;
            int rows = Mathf.CeilToInt(field.size.y / tileH) + 1;
            float startX = field.min.x + tileW * 0.5f;
            float startY = field.min.y + tileH * 0.5f;

            var floorRoot = CreateChild(root, "LabFloor");
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    string name = FloorTiles[rng.Next(FloorTiles.Length)];
                    var sprite = LabTileset.Get(name);
                    if (sprite == null) continue;

                    var sr = CreateRenderer(floorRoot, $"{name}_{cx}_{cy}", sprite, FloorOrder);
                    // タイルの中心ピボットをそのまま格子に敷き詰める
                    sr.transform.position = new Vector3(startX + cx * tileW, startY + cy * tileH, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- 壁

        private void BuildWallRow(Transform root, Bounds field, System.Random rng)
        {
            var wallRoot = CreateChild(root, "LabWall");
            float cursor = field.min.x;
            float topY   = field.max.y;

            int guard = 0;
            while (cursor < field.max.x - 0.5f && guard++ < 64)
            {
                string name = WallSegments[rng.Next(WallSegments.Length)];
                var sprite = LabTileset.Get(name);
                if (sprite == null) break;

                float w = sprite.bounds.size.x;
                if (cursor + w > field.max.x + 0.25f)
                {
                    // 残り幅が細い場合は最後の1枚を引き伸ばして閉じる
                    float remain = field.max.x - cursor;
                    if (remain < w * 0.35f) break;
                    var stretch = CreateRenderer(wallRoot, $"{name}_end", sprite, WallOrder);
                    stretch.transform.position = new Vector3(cursor + remain * 0.5f, topY, 0f);
                    stretch.transform.localScale = new Vector3(remain / w, 1f, 1f);
                    break;
                }

                var sr = CreateRenderer(wallRoot, $"{name}_{guard}", sprite, WallOrder);
                // 壁スプライトは下端ピボットなので topY に立てる
                sr.transform.position = new Vector3(cursor + w * 0.5f, topY, 0f);
                cursor += w;
            }
        }

        // ---------------------------------------------------------------- 小物

        private void BuildEdgeProps(Transform root, Bounds field, Bounds route)
        {
            var propRoot = CreateChild(root, "LabProps");
            float clearance = _propClearance;

            // 左右の縦ストリップ（ルートの外側〜フィールド端）
            float leftX  = Mathf.Lerp(field.min.x + 1.2f, route.min.x - clearance, 0.5f);
            float rightX = Mathf.Lerp(route.max.x + clearance, field.max.x - 1.2f, 0.5f);
            float stripTop = field.max.y - 3.2f; // 壁の分だけ下げる
            PlaceVerticalStrip(propRoot, leftX,  field.min.y + 1.0f, stripTop, route, "L");
            PlaceVerticalStrip(propRoot, rightX, field.min.y + 1.0f, stripTop, route, "R");

            // 下端の横ストリップ
            float bottomY = Mathf.Lerp(field.min.y + 1.0f, route.min.y - clearance, 0.5f);
            PlaceHorizontalStrip(propRoot, bottomY, field.min.x + 1.2f, field.max.x - 1.2f, route, "B");
        }

        private void PlaceVerticalStrip(Transform parent, float x, float yMin, float yMax,
            Bounds route, string tag)
        {
            if (yMax - yMin < 1.5f) return;

            float y = yMin;
            int guard = 0;
            while (y < yMax && guard++ < 24)
            {
                string name = NextProp();
                var sprite = LabTileset.Get(name);
                if (sprite == null) return;

                var size = sprite.bounds.size;
                if (y + size.y > yMax) break;
                if (size.x > StripWidth(route)) { y += size.y * 0.5f; continue; }

                var pos = new Vector3(x, y, 0f);
                if (IntersectsRoute(pos, size, route)) { y += size.y * 0.6f; continue; }

                var sr = CreateRenderer(parent, $"{name}_{tag}{guard}", sprite, PropOrder);
                sr.transform.position = pos; // 下端ピボットで y が底辺
                y += size.y + _propGap;
            }
        }

        private void PlaceHorizontalStrip(Transform parent, float y, float xMin, float xMax,
            Bounds route, string tag)
        {
            if (xMax - xMin < 1.5f) return;

            float x = xMin;
            int guard = 0;
            while (x < xMax && guard++ < 24)
            {
                string name = NextProp();
                var sprite = LabTileset.Get(name);
                if (sprite == null) return;

                var size = sprite.bounds.size;
                if (x + size.x > xMax) break;

                var pos = new Vector3(x + size.x * 0.5f, y, 0f);
                if (IntersectsRoute(pos, size, route)) { x += size.x * 0.6f; continue; }

                var sr = CreateRenderer(parent, $"{name}_{tag}{guard}", sprite, PropOrder);
                sr.transform.position = pos;
                x += size.x + _propGap;
            }
        }

        private float StripWidth(Bounds route) => 6.5f;

        private static bool IntersectsRoute(Vector3 bottomCenter, Vector2 size, Bounds route)
        {
            var min = new Vector2(bottomCenter.x - size.x * 0.5f, bottomCenter.y);
            var max = new Vector2(bottomCenter.x + size.x * 0.5f, bottomCenter.y + size.y);
            var expanded = new Bounds(route.center, route.size + new Vector3(2.8f, 2.8f, 0f));
            return max.x > expanded.min.x && min.x < expanded.max.x &&
                   max.y > expanded.min.y && min.y < expanded.max.y;
        }

        // ---------------------------------------------------------------- 共通

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string objectName, Sprite sprite, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Background, order);
            return sr;
        }
    }
}
