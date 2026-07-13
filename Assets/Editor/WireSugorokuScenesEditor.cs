using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.UI;

namespace Sugoroku.Editor
{
    public static class WireSugorokuScenesEditor
    {
        private const string GameUIScenePath = "Assets/Scenes/GameUIScene.unity";

        public static void WireGameUIScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("GameUIScene.unity"))
                scene = EditorSceneManager.OpenScene(GameUIScenePath, OpenSceneMode.Single);

            WireGameHud(FindComponent<GameHUD>());
            WireEventModal(FindComponent<EventModalUI>());
            WirePause(FindComponent<PauseMenuUI>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ GameUIScene の UI 参照を接続しました。");
        }

        private static void WireGameHud(GameHUD hud)
        {
            if (hud == null) { Debug.LogWarning("GameHUD が見つかりません。"); return; }

            var so = new SerializedObject(hud);
            var bar = hud.transform.parent?.Find("ResourceBar");
            if (bar != null)
            {
                AssignTmpFrom(so, "_moneyText", bar.Find("MoneyText"));
                AssignTmpFrom(so, "_ifScoreText", bar.Find("IfScoreText"));
                AssignTmpFrom(so, "_mentalText", bar.Find("MentalText"));
                AssignTmpFrom(so, "_virtueText", bar.Find("VirtueText"));
            }
            AssignTmp(so, "_playerNameText", "PlayerNameText", hud.transform);
            AssignTmp(so, "_turnStateText", "HudText", hud.transform);
            AssignTmp(so, "_goalDistanceText", "GoalDistanceText", hud.transform);
            AssignTmp(so, "_tuitionDistanceText", "TuitionDistanceText", hud.transform);
            AssignTmp(so, "_skipTurnsText", "SkipTurnsText", hud.transform);
            AssignTmp(so, "_ignoreEventsText", "IgnoreEventsText", hud.transform);
            AssignSlider(so, "_mentalSlider", "MentalSlider", hud.transform);
            AssignImage(so, "_diceIconImage", "DiceIcon");
            AssignTmp(so, "_diceResultText", "DiceResult");
            AssignTmp(so, "_logText", "LogText");
            AssignButton(so, "_rollButton", "RollButton");
            AssignButton(so, "_skillButton", "SkillButton");
            AssignButton(so, "_menuButton", "MenuButton");
            AssignChildTmp(so, "_skillButtonText", "SkillButton", "Label");
            AssignComponent(so, "_pauseMenu", FindComponent<PauseMenuUI>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireEventModal(EventModalUI modal)
        {
            if (modal == null) { Debug.LogWarning("EventModalUI が見つかりません。"); return; }

            var so = new SerializedObject(modal);
            AssignGameObject(so, "_panel", modal.gameObject);
            AssignTmp(so, "_titleText", "ModalTitle", modal.transform);
            AssignTmp(so, "_tagsText", "ModalTags", modal.transform);
            AssignTmp(so, "_descriptionText", "ModalDescription", modal.transform);
            AssignTransform(so, "_choiceButtonParent", "ChoiceButtonParent", modal.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePause(PauseMenuUI pause)
        {
            if (pause == null) { Debug.LogWarning("PauseMenuUI が見つかりません。"); return; }

            var so = new SerializedObject(pause);
            AssignGameObject(so, "_panel", pause.gameObject);
            AssignTmp(so, "_scoreBreakdownText", "ScoreBreakdownText", pause.transform);
            AssignButton(so, "_resumeButton", "ResumeButton", pause.transform);
            AssignButton(so, "_titleButton", "TitleButton", pause.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindComponent<T>() where T : Object =>
            Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        private static void AssignGameObject(SerializedObject so, string prop, GameObject value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        private static void AssignComponent<T>(SerializedObject so, string prop, T value) where T : Object
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        private static void AssignTmpFrom(SerializedObject so, string prop, Transform t)
        {
            var p = so.FindProperty(prop);
            if (p != null && t != null) p.objectReferenceValue = t.GetComponent<TextMeshProUGUI>();
        }

        private static void AssignTmp(SerializedObject so, string prop, string childName, Transform root = null)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            var t = root != null ? root.Find(childName) : null;
            if (t == null)
            {
                var go = GameObject.Find(childName);
                t = go != null ? go.transform : null;
            }
            if (t != null) p.objectReferenceValue = t.GetComponent<TextMeshProUGUI>();
        }

        private static void AssignChildTmp(SerializedObject so, string prop, string parentName, string childName)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            var parent = GameObject.Find(parentName);
            if (parent == null) return;
            var child = parent.transform.Find(childName);
            if (child != null) p.objectReferenceValue = child.GetComponent<TextMeshProUGUI>();
        }

        private static void AssignImage(SerializedObject so, string prop, string objectName, Transform root = null)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            Transform t = root != null ? root.Find(objectName) : null;
            if (t == null)
            {
                var go = GameObject.Find(objectName);
                t = go != null ? go.transform : null;
            }
            if (t != null) p.objectReferenceValue = t.GetComponent<Image>();
        }

        private static void AssignButton(SerializedObject so, string prop, string objectName, Transform root = null)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            Transform t = root != null ? root.Find(objectName) : null;
            if (t == null)
            {
                var go = GameObject.Find(objectName);
                t = go != null ? go.transform : null;
            }
            if (t != null) p.objectReferenceValue = t.GetComponent<Button>();
        }

        private static void AssignSlider(SerializedObject so, string prop, string childName, Transform root)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            var t = root.Find(childName);
            if (t != null) p.objectReferenceValue = t.GetComponent<Slider>();
        }

        private static void AssignTransform(SerializedObject so, string prop, string childName, Transform root)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            var t = root.Find(childName);
            if (t != null) p.objectReferenceValue = t;
        }
    }
}
