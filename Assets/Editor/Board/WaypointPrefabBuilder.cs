using System.IO;
using UnityEditor;
using UnityEngine;
using Sugoroku.Board;
using Sugoroku.Data;

namespace Sugoroku.Editor.Board
{
    public static class WaypointPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/Board";
        public const string PrefabPath           = PrefabFolder + "/Waypoint_Base.prefab";
        public const string MassTextCardPrefabPath = PrefabFolder + "/Mass_TextCard.prefab";

        public static void CreateWaypointBasePrefab()
        {
            BoardSortingLayerSetup.EnsureSortingLayers();
            if (!Directory.Exists(PrefabFolder))
                Directory.CreateDirectory(PrefabFolder);

            var go = new GameObject("Waypoint_Base");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BoardVisualUtility.GetSquareSprite();
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Board, BoardSortingLayers.WaypointBaseOrder);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.45f;

            var wp = go.AddComponent<Waypoint>();
            wp.Configure(0, SquareType.Start);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            Selection.activeObject = prefab;
            Debug.Log($"✅ Waypoint_Base プレハブを作成しました: {PrefabPath}");
        }

        public static void CreateMassTextCardPrefab()
        {
            BoardSortingLayerSetup.EnsureSortingLayers();
            if (!Directory.Exists(PrefabFolder))
                Directory.CreateDirectory(PrefabFolder);

            var root = new GameObject("Mass_TextCard");
            var wp = root.AddComponent<Waypoint>();
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(MassTextCardPrefabFactory.CardWorldWidth, MassTextCardPrefabFactory.CardWorldHeight);

            MassTextCardPrefabFactory.BuildCardUi(root.transform);
            var card = root.AddComponent<MassTextCardView>();
            wp.Configure(0, SquareType.Event, null, "E012");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MassTextCardPrefabPath);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            Debug.Log($"✅ Mass_TextCard プレハブを作成しました: {MassTextCardPrefabPath}");
        }

        public static void SetupProject()
        {
            BoardSortingLayerSetup.EnsureSortingLayers();
            if (AssetDatabase.LoadAssetAtPath<Waypoint>(MassTextCardPrefabPath) == null)
                CreateMassTextCardPrefab();
            else if (AssetDatabase.LoadAssetAtPath<Waypoint>(PrefabPath) == null)
                CreateWaypointBasePrefab();
            else
                Debug.Log("盤面プレハブは既に存在します。");
        }
    }
}
