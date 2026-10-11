using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Visual
{
    /// <summary>
    /// Resources/goods の研究室タイルセット（JSON+PNG）を実行時にスライスして供給する。
    /// PNG はエディタの Importer 未適用（spriteMode: None）のため Sprite.Create で動的生成する。
    /// </summary>
    public static class LabTileset
    {
        private const string TextureResource = "goods/a_pixel_art_tileset_in_2d_top_down_view_showcases";
        private const string JsonResource    = "goods/laboratory_tileset";

        [System.Serializable] private class SheetMeta   { public string image; public SheetSize size; public float pixelsPerUnit = 32f; }
        [System.Serializable] private class SheetSize   { public int w; public int h; }
        [System.Serializable] private class FramePivot  { public float x = 0.5f; public float y = 0.5f; }
        [System.Serializable] private class Frame       { public string name; public float x; public float y; public float w; public float h; public FramePivot pivot; }
        [System.Serializable] private class Sheet       { public SheetMeta meta; public Frame[] frames; }

        private static readonly Dictionary<string, Sprite> _cache = new();
        private static bool   _loadAttempted;
        private static float  _pixelsPerUnit = 32f;

        /// <summary>スプライトが1つでも読み込めていれば true。</summary>
        public static bool IsReady => _cache.Count > 0;

        public static void WarmUp() => EnsureLoaded();

        /// <summary>タイルセット内のスプライトを名前で取得する。未定義名は null。</summary>
        public static Sprite Get(string spriteName)
        {
            EnsureLoaded();
            return spriteName != null && _cache.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }

        /// <summary>スプライトのワールド幅（PPU換算）。未定義名は 0。</summary>
        public static float GetWorldWidth(string spriteName) => Get(spriteName) is { } s ? s.bounds.size.x : 0f;

        /// <summary>スプライトのワールド高さ（PPU換算）。未定義名は 0。</summary>
        public static float GetWorldHeight(string spriteName) => Get(spriteName) is { } s ? s.bounds.size.y : 0f;

        private static void EnsureLoaded()
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            var json = Resources.Load<TextAsset>(JsonResource);
            var tex  = Resources.Load<Texture2D>(TextureResource);
            if (json == null || tex == null)
            {
                Debug.LogWarning($"[LabTileset] 素材が見つかりません: {JsonResource} / {TextureResource}");
                return;
            }

            var sheet = JsonUtility.FromJson<Sheet>(json.text);
            if (sheet?.frames == null || sheet.frames.Length == 0)
            {
                Debug.LogWarning("[LabTileset] JSONにframesがありません。");
                return;
            }

            if (sheet.meta != null && sheet.meta.pixelsPerUnit > 0f)
                _pixelsPerUnit = sheet.meta.pixelsPerUnit;

            foreach (var frame in sheet.frames)
            {
                if (frame == null || string.IsNullOrEmpty(frame.name) || frame.w <= 0f || frame.h <= 0f)
                    continue;

                // JSONは左上原点。Unityテクスチャ座標（左下原点）へ変換する。
                float unityY = tex.height - frame.y - frame.h;
                var pivot = new Vector2(
                    frame.pivot != null ? Mathf.Clamp01(frame.pivot.x) : 0.5f,
                    frame.pivot != null ? Mathf.Clamp01(frame.pivot.y) : 0.5f);

                var sprite = Sprite.Create(tex, new Rect(frame.x, unityY, frame.w, frame.h), pivot, _pixelsPerUnit);
                sprite.name = frame.name;
                _cache[frame.name] = sprite;
            }
        }
    }
}
