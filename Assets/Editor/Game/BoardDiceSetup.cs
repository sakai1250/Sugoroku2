using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sugoroku.Game;
using Sugoroku.Visual;

namespace Sugoroku.Editor.Game
{
    public static class BoardDiceSetup
    {
        public static void EnsureInScene()
        {
            if (Object.FindFirstObjectByType<BoardDice>() != null)
                return;

            var roller = Object.FindFirstObjectByType<DiceRoller>();
            if (roller == null)
            {
                Debug.LogWarning("DiceRoller が見つかりません。GameWorldScene を開いてください。");
                return;
            }

            var diceGo = new GameObject("Dice");
            diceGo.transform.position = new Vector3(9f, 1f, 0f);

            var sr = diceGo.AddComponent<SpriteRenderer>();
            var faces = KenneyAssets.LoadDiceFaces("dieWhite");
            if (faces.Length >= 6) sr.sprite = faces[5];

            var box = diceGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.1f, 1.1f);

            var boardDice = diceGo.AddComponent<BoardDice>();

            var so = new SerializedObject(roller);
            var prop = so.FindProperty("_boardDice");
            if (prop != null)
            {
                prop.objectReferenceValue = boardDice;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
