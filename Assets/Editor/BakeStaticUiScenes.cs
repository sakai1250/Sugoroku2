using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sugoroku.UI;
using Sugoroku.Data;

namespace Sugoroku.Editor
{
    /// <summary>
    /// Create UI hierarchy in the Editor, serialize it into the actual *.unity scenes,
    /// then mark the Canvas so playback cannot overwrite manually edited positions.
    /// Never regenerates GameWorldScene or changes game data.
    /// </summary>
    public static class BakeStaticUiScenes
    {
        private const string Scenes = "Assets/Scenes/";

        [MenuItem("Tools/UI/1. 全画面を静的化してSceneに保存")]
        public static void BakeAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play mode before baking static UI.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string original = SceneManager.GetActiveScene().path;
            try
            {
                Bake(Scenes + "TitleScene.unity", BakeTitle);
                Bake(Scenes + "CharacterSelectScene.unity", BakeCharacterSelect);
                Bake(Scenes + "GameUIScene.unity", BakeGameUI);
                Bake(Scenes + "ResultScene.unity", BakeResult);
                Bake(Scenes + "GameOverScene.unity", BakeGameOver);
                AssetDatabase.SaveAssets();
                Debug.Log("[Static UI] 5 scenes baked. Hierarchy and RectTransforms are now serialized in *.unity files.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(original) && File.Exists(original))
                    EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            }
        }

        private static void Bake(string path, Action action)
        {
            EditorUtility.DisplayProgressBar("Static UI", path, 0.45f);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            // When re-baking, remove the preservation switch temporarily.
            foreach (var marker in UnityEngine.Object.FindObjectsByType<StaticUiAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(marker);

            action();
            // Resolve the main Canvas before adding the separate help Canvas.
            var canvas = FindMainCanvas();
            if (canvas == null) throw new InvalidOperationException("Canvas missing: " + path);
            BakeHelpOverlay();
            canvas.gameObject.AddComponent<StaticUiAuthoring>();
            FixLongText(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Scene could not be saved: " + path);
            Debug.Log("[Static UI] saved " + path);
        }

        private static void BakeTitle()
        {
            // Most title controls already exist. Complete the previously dynamic parts.
            var root = UnityEngine.Object.FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include);
            if (root == null) throw new InvalidOperationException("TitleMenuController missing");
            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Title Canvas missing");
            SetField(root, "_tapToStartPanel", root.transform.Find("TapToStartPanel")?.gameObject);
            SetField(root, "_titlePanel", root.transform.Find("TitlePanel")?.gameObject);
            SetField(root, "_settingsPanel", root.transform.Find("SettingsPanel")?.gameObject);
            SetField(root, "_achievementsPanel", root.transform.Find("AchievementsPanel")?.gameObject);
            Invoke(root, "EnsureTapToStartPanel");
            Invoke(root, "EnsureSettingsControls");
            Invoke(root, "EnsureDailyChallengeButton");
            Invoke(root, "EnsureHelpButton");
            Invoke(root, "EnsureBackButton");
            Invoke(root, "EnsureTitleTagline");
            Invoke(root, "EnsureDailyCaption");
            Invoke(root, "ApplySafeLayout");
            Invoke(root, "ApplyScreenChrome");

            var settings = root.transform.Find("SettingsPanel") as RectTransform;
            if (settings != null)
            {
                settings.sizeDelta = new Vector2(Mathf.Max(settings.sizeDelta.x, 860f),
                    Mathf.Max(settings.sizeDelta.y, 830f));
            }
            var background = canvas.transform.Find("TitleBackgroundArt");
            if (background != null) background.SetAsFirstSibling();
        }

        private static void BakeCharacterSelect()
        {
            var c = UnityEngine.Object.FindFirstObjectByType<CharacterSelectController>(FindObjectsInactive.Include);
            if (c == null) throw new InvalidOperationException("CharacterSelectController missing");
            Invoke(c, "BindReferences");
            SetField(c, "_selected", CharacterType.Hobbyist);
            SetField(c, "_index", 0);
            Invoke(c, "EnsureMenuOverlayAndButtons");
            Invoke(c, "ApplySceneLayout");
            Invoke(c, "BuildCardButtons");
            Invoke(c, "ApplyScreenChrome");
            Invoke(c, "ApplySceneLayout");

            var overlay = c.GetComponent<CharacterSelectMenuOverlay>();
            if (overlay != null)
            {
                var canvas = c.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.transform.Find("SettingsModalOverlay") == null)
                    Invoke(overlay, "BuildSettingsModal");
                if (canvas != null && canvas.transform.Find("AchievementsModalOverlay") == null)
                    Invoke(overlay, "BuildAchievementsModal");
                if (canvas != null)
                {
                    Hide(canvas.transform, "SettingsModalOverlay");
                    Hide(canvas.transform, "AchievementsModalOverlay");
                }
            }
        }

        private static void BakeGameUI()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>(FindObjectsInactive.Include);
            if (hud == null) throw new InvalidOperationException("GameHUD missing");
            var canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("GameUI Canvas missing");
            var layout = canvas.GetComponent<GameMainLayout>() ?? canvas.gameObject.AddComponent<GameMainLayout>();
            layout.ApplyLayout();
            Invoke(hud, "BindAllReferences");
            Invoke(hud, "SetupSpeedToggleButton");
            var pause = UnityEngine.Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (pause != null)
            {
                var panel = pause.transform;
                SetField(pause, "_panel", pause.gameObject);
                var status = panel.Find("PauseTitle")?.GetComponent<TextMeshProUGUI>();
                if (status != null) SetField(pause, "_statusTitleText", status);
                var report = panel.Find("ScoreBreakdownText")?.GetComponent<TextMeshProUGUI>();
                if (report == null)
                {
                    var go = NewUI(panel, "ScoreBreakdownText", text: "ゲーム進行中の成績・スキルの詳細を表示します。\n\n長文は縦スクロールできます。");
                    report = go.GetComponent<TextMeshProUGUI>();
                    report.fontSize = 18f;
                    report.alignment = TextAlignmentOptions.TopLeft;
                }
                SetField(pause, "_scoreBreakdownText", report);
                var resume = panel.Find("ResumeButton")?.GetComponent<Button>();
                var title = panel.Find("TitleButton")?.GetComponent<Button>();
                if (resume != null) SetField(pause, "_resumeButton", resume);
                if (title != null) SetField(pause, "_titleButton", title);
                Invoke(pause, "CreateHelpButton");
                SetField(pause, "_helpButton", panel.Find("HelpButton")?.GetComponent<Button>());
                Invoke(pause, "ApplyChrome");
                Invoke(pause, "ApplySafeLayout");
            }
            // The event description and choice buttons change with game events.
            // Bake only the reusable shell; leave data-dependent rows dynamic.
            var eventModal = UnityEngine.Object.FindFirstObjectByType<EventModalUI>(FindObjectsInactive.Include);
            if (eventModal != null)
            {
                Invoke(eventModal, "EnsureModalShell");
                EventModalLayout.Apply(eventModal.transform, null);
                eventModal.gameObject.SetActive(false);
            }
            KenneyUiStyler.StyleCanvas(canvas);
            UiSafeLayout.DisableDecorativeRaycasts(canvas.transform);
        }

        private static void BakeResult()
        {
            var result = UnityEngine.Object.FindFirstObjectByType<ResultSceneController>(FindObjectsInactive.Include);
            if (result == null) throw new InvalidOperationException("ResultSceneController missing");
            EndSceneVisuals.ApplyGraduation(result.transform, null);
            ResultSceneLayout.Apply(result.transform);
            Invoke(result, "EnsureShareButtons");
            var shareRow = result.transform.Find("ShareButtonRow") as RectTransform;
            if (shareRow != null)
            {
                shareRow.sizeDelta = new Vector2(920f, 84f);
                foreach (Transform button in shareRow)
                    button.gameObject.SetActive(true); // Visible and editable in Scene view.
            }
            // Static empty plot: runtime only replaces its data points.
            if (result.transform.Find("StatHistoryChart") == null)
            {
                var chart = NewUI(result.transform, "StatHistoryChart", image: true);
                var rt = (RectTransform)chart.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-36f, -138f);
                rt.sizeDelta = new Vector2(350f, 160f);
                chart.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.92f);
                chart.GetComponent<Image>().raycastTarget = false;
                var label = NewUI(rt, "ChartLabel", text: "IF・メンタルの推移");
                var l = label.GetComponent<TextMeshProUGUI>();
                l.fontSize = 16f;
                l.alignment = TextAlignmentOptions.TopLeft;
                var lrt = l.rectTransform;
                lrt.anchorMin = new Vector2(0f, 1f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, -6f);
                lrt.sizeDelta = new Vector2(-16f, 24f);
                var plot = NewUI(rt, "PlotArea");
                var prt = (RectTransform)plot.transform;
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = new Vector2(12f, 12f);
                prt.offsetMax = new Vector2(-12f, -32f);
            }
        }

        private static void BakeGameOver()
        {
            var root = UnityEngine.Object.FindFirstObjectByType<GameOverSceneController>(FindObjectsInactive.Include);
            if (root == null) throw new InvalidOperationException("GameOverSceneController missing");
            Invoke(root, "EnsureRetryButton");
            var preview = GameOverOutcomeResolver.Resolve(GameOverReason.Bankruptcy);
            EndSceneVisuals.ApplyGameOver(root.transform, preview);
            GameOverLayout.ApplyContent(root.transform, GameOverVisualStyle.BankruptcyNotice);
            var body = root.transform.Find("GameOverBody")?.GetComponent<TextMeshProUGUI>();
            if (body != null)
            {
                body.enableAutoSizing = true;
                body.fontSizeMin = 13f;
                body.fontSizeMax = 20f;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static Canvas FindMainCanvas()
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
                if (c.GetComponentInParent<HelpPanelUI>(true) == null &&
                    c.GetComponentInParent<EventModalUI>(true) == null &&
                    c.transform.parent == null)
                    return c;
            foreach (var c in canvases)
                if (c.GetComponentInParent<HelpPanelUI>(true) == null &&
                    c.GetComponentInParent<EventModalUI>(true) == null)
                    return c;
            return null;
        }

        private static void BakeHelpOverlay()
        {
            var help = UnityEngine.Object.FindFirstObjectByType<HelpPanelUI>(FindObjectsInactive.Include);
            if (help == null)
            {
                var root = new GameObject("HelpPanelUI");
                help = root.AddComponent<HelpPanelUI>();
            }
            if (help.transform.Find("HelpOverlayCanvas") == null)
                Invoke(help, "BuildOverlay");
            Hide(help.transform, "HelpOverlayCanvas");
        }

        private static GameObject NewUI(Transform parent, string name, bool image = false, string text = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (image) go.AddComponent<Image>();
            if (text != null)
            {
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = text;
                tmp.color = Color.white;
                JapaneseFontProvider.Apply(tmp);
            }
            return go;
        }

        private static void Hide(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        private static void FixLongText(Canvas canvas)
        {
            foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.rectTransform.rect.width < 1f) continue;
                if (tmp.GetComponentInParent<Button>() != null)
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMax = Mathf.Max(tmp.fontSize, 12f);
                    tmp.fontSizeMin = Mathf.Max(10f, tmp.fontSizeMax * 0.72f);
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                }
                else if (tmp.name.Contains("Description") || tmp.name.Contains("Body") || tmp.name.Contains("Desc"))
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMax = Mathf.Max(tmp.fontSize, 14f);
                    tmp.fontSizeMin = Mathf.Max(11f, tmp.fontSizeMax * 0.70f);
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                }
                tmp.raycastTarget = false;
            }
        }

        private static void Invoke(Component component, string method)
        {
            var mi = component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (mi == null) throw new MissingMethodException(component.GetType().Name, method);
            try { mi.Invoke(component, null); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        private static void SetField(Component component, string name, object value)
        {
            var f = component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException(component.GetType().Name, name);
            f.SetValue(component, value);
        }
    }
}
