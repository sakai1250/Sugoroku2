using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>修了／ゲームオーバー画面のビジュアル差分（screen.md §5.1 / §5.2）。</summary>
    public static class EndSceneVisuals
    {
        public static void ApplyGraduation(Transform root, GraduationOutcome? heroOutcome)
        {
            if (root == null) return;

            var panel = root.GetComponent<Image>();
            if (panel != null)
                panel.color = new Color(0.05f, 0.08f, 0.14f, 0.98f);

            if (heroOutcome == null) return;

            var accent = root.Find("AccentBanner")?.GetComponent<Image>();
            if (accent == null)
            {
                var go = new GameObject("AccentBanner", typeof(RectTransform));
                go.transform.SetParent(root, false);
                go.transform.SetAsFirstSibling();
                accent = go.AddComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 8f);
                rt.anchoredPosition = Vector2.zero;
            }

            accent.color = heroOutcome.Value.AccentColor;

            var title = root.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
                title.color = heroOutcome.Value.AccentColor;
        }

        public static void ApplyGameOver(Transform root, GameOverOutcome outcome)
        {
            if (root == null) return;

            var panel = root.GetComponent<Image>();
            if (panel != null)
                panel.color = outcome.BackgroundTint;

            var title = root.Find("GameOverTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
                title.color = outcome.AccentColor;

            var body = root.Find("GameOverBody")?.GetComponent<TextMeshProUGUI>();
            if (body != null)
            {
                body.color = new Color(0.82f, 0.82f, 0.88f);
                body.raycastTarget = false;
            }
        }
    }
}
