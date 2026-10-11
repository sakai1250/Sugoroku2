using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Visual
{
    public enum CampusZone
    {
        Laboratory,     // 序盤・研究室（実験台、PC、顕微鏡、本棚、遠心分離機）
        LectureRoom,    // 中盤・講義室・ゼミ室（教壇、黒板、長机、椅子、プロジェクター）
        ConferenceHall, // 終盤・学会・国際会議（演壇、ポスターセッション、受付）
        Izakaya         // 分岐・居酒屋・繁華街（畳、のれん、座敷、ビール、料理）
    }

    /// <summary>
    /// Resources/goods に含まれる 4 大キャンパスタイルセット
    /// （研究室 75種、講義室 85種、学会ホール 111種、居酒屋 103種、計374種）
    /// を実行時に統一ロード・スライスして供給する総合アセットマネージャー。
    /// </summary>
    public static class CampusTilesetManager
    {
        [Serializable]
        private class RectDef
        {
            public string name;
            public float x;
            public float y;
            public float w;
            public float h;
            public float width;
            public float height;
            public PivotDef pivot;

            public float ActualWidth => width > 0f ? width : w;
            public float ActualHeight => height > 0f ? height : h;
        }

        [Serializable]
        private class PivotDef
        {
            public float x = 0.5f;
            public float y = 0.5f;
        }

        private static readonly Dictionary<string, Sprite> _allSprites = new();
        private static readonly Dictionary<CampusZone, List<Sprite>> _zoneFloors = new();
        private static readonly Dictionary<CampusZone, List<Sprite>> _zoneWalls = new();
        private static readonly Dictionary<CampusZone, List<Sprite>> _zoneProps = new();

        private static bool _loaded;

        public static bool IsReady
        {
            get
            {
                EnsureLoaded();
                return _allSprites.Count > 0;
            }
        }

        public static int TotalSpriteCount
        {
            get
            {
                EnsureLoaded();
                return _allSprites.Count;
            }
        }

        public static void WarmUp() => EnsureLoaded();

        public static Sprite GetSprite(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(name)) return null;
            return _allSprites.TryGetValue(name, out var s) ? s : null;
        }

        public static IReadOnlyList<Sprite> GetFloors(CampusZone zone)
        {
            EnsureLoaded();
            return _zoneFloors.TryGetValue(zone, out var list) ? list : Array.Empty<Sprite>();
        }

        public static IReadOnlyList<Sprite> GetWalls(CampusZone zone)
        {
            EnsureLoaded();
            return _zoneWalls.TryGetValue(zone, out var list) ? list : Array.Empty<Sprite>();
        }

        public static IReadOnlyList<Sprite> GetProps(CampusZone zone)
        {
            EnsureLoaded();
            return _zoneProps.TryGetValue(zone, out var list) ? list : Array.Empty<Sprite>();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            foreach (CampusZone z in Enum.GetValues(typeof(CampusZone)))
            {
                _zoneFloors[z] = new List<Sprite>();
                _zoneWalls[z] = new List<Sprite>();
                _zoneProps[z] = new List<Sprite>();
            }

            // 1. 研究室タイルセット (laboratory_tileset)
            LoadTileset(
                "goods/laboratory_tileset",
                "goods/a_pixel_art_tileset_in_2d_top_down_view_showcases",
                CampusZone.Laboratory,
                isFramesFormat: true);

            // 2. 講義室タイルセット (lecture_room_tileset)
            LoadTileset(
                "goods/lecture_room_tileset",
                "goods/教室_講義室アセットシート",
                CampusZone.LectureRoom,
                isFramesFormat: false);

            // 3. 学会・会議ホール (conference_hall_tileset)
            LoadTileset(
                "goods/conference_hall_tileset",
                "goods/会議ホールのピクセルアート素材",
                CampusZone.ConferenceHall,
                isFramesFormat: false);

            // 4. 居酒屋・繁華街 (izakaya_tileset)
            LoadTileset(
                "goods/izakaya_tileset",
                "goods/居酒屋の内装アセットシート",
                CampusZone.Izakaya,
                isFramesFormat: false);

            Debug.Log($"[CampusTilesetManager] 全キャンパスタイルセット読み込み完了: 計 {_allSprites.Count} 点");
        }

        private static void LoadTileset(string jsonResource, string textureResource, CampusZone zone, bool isFramesFormat)
        {
            var json = Resources.Load<TextAsset>(jsonResource);
            var tex = Resources.Load<Texture2D>(textureResource);

            // NFD/NFC等で直接取得できない場合のフォールバック（LoadAll）
            if (tex == null)
            {
                var allTex = Resources.LoadAll<Texture2D>("goods");
                string shortName = textureResource.Replace("goods/", "");
                foreach (var t in allTex)
                {
                    if (string.Equals(t.name.Normalize(),
                                      shortName.Normalize(),
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        tex = t;
                        break;
                    }
                }
            }

            if (json == null || tex == null)
            {
                Debug.LogWarning($"[CampusTilesetManager] タイルセットが見つかりません: {jsonResource} / {textureResource}");
                return;
            }

            ParseAndSlice(json.text, tex, zone, isFramesFormat);
        }

        private static void ParseAndSlice(string jsonText, Texture2D tex, CampusZone zone, bool isFramesFormat)
        {
            // JsonUtility用ラップ定義
            List<(string name, float x, float y, float w, float h, float px, float py)> rects = new();

            try
            {
                // laboratory_tileset.json (frames/w/h)
                if (isFramesFormat)
                {
                    var labSheet = JsonUtility.FromJson<LabSheet>(jsonText);
                    if (labSheet?.frames != null)
                    {
                        foreach (var f in labSheet.frames)
                        {
                            if (f == null || string.IsNullOrEmpty(f.name)) continue;
                            float px = f.pivot != null ? f.pivot.x : 0.5f;
                            float py = f.pivot != null ? f.pivot.y : 0.5f;
                            rects.Add((f.name, f.x, f.y, f.w, f.h, px, py));
                        }
                    }
                }
                else
                {
                    // sprites/width/height format
                    var standardSheet = JsonUtility.FromJson<StandardSheet>(jsonText);
                    if (standardSheet?.sprites != null)
                    {
                        foreach (var s in standardSheet.sprites)
                        {
                            if (s == null || string.IsNullOrEmpty(s.name)) continue;
                            float px = s.pivot != null ? s.pivot.x : 0.5f;
                            float py = s.pivot != null ? s.pivot.y : 0.5f;
                            rects.Add((s.name, s.x, s.y, s.width, s.height, px, py));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CampusTilesetManager] JSONパース失敗 ({zone}): {ex.Message}");
                return;
            }

            const float ppu = 32f;
            foreach (var r in rects)
            {
                if (r.w <= 0f || r.h <= 0f) continue;

                // Unity座標変換（左上原点 -> 左下原点）
                float unityY = tex.height - r.y - r.h;
                var pivot = new Vector2(Mathf.Clamp01(r.px), Mathf.Clamp01(r.py));

                var sprite = Sprite.Create(tex, new Rect(r.x, unityY, r.w, r.h), pivot, ppu);
                sprite.name = r.name;
                _allSprites[r.name] = sprite;

                ClassifySprite(zone, r.name, sprite);
            }
        }

        private static void ClassifySprite(CampusZone zone, string name, Sprite sprite)
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("floor") || lower.Contains("tatami") || lower.Contains("carpet") || lower.Contains("tile"))
            {
                _zoneFloors[zone].Add(sprite);
            }
            else if (lower.Contains("wall") || lower.Contains("window") || lower.Contains("door") || lower.Contains("column") || lower.Contains("noren"))
            {
                _zoneWalls[zone].Add(sprite);
            }
            else
            {
                _zoneProps[zone].Add(sprite);
            }
        }

        // JSONマッピング用内部クラス
        [Serializable]
        private class LabSheet
        {
            public LabFrame[] frames;
        }

        [Serializable]
        private class LabFrame
        {
            public string name;
            public float x;
            public float y;
            public float w;
            public float h;
            public PivotDef pivot;
        }

        [Serializable]
        private class StandardSheet
        {
            public StandardSprite[] sprites;
        }

        [Serializable]
        private class StandardSprite
        {
            public string name;
            public float x;
            public float y;
            public float width;
            public float height;
            public PivotDef pivot;
        }
    }
}
