using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.UI
{
    /// <summary>screen.md §3.2 — 4大リソース HUD の表記とアイコン色。</summary>
    public static class ResourceHudVisuals
    {
        public const float TopBarHeight = 90f;

        public static readonly Color MoneyTextColor  = new(1f, 0.92f, 0.45f);
        public static readonly Color IfTextColor     = new(0.55f, 0.85f, 1f);
        public static readonly Color MentalTextColor = new(0.75f, 0.95f, 0.8f);
        public static readonly Color VirtueTextColor  = new(0.82f, 0.65f, 1f);
        public static readonly Color PinchTextColor   = new(1f, 0.22f, 0.22f, 1f);

        public static readonly Color MoneyIconTint  = new(1f, 0.82f, 0.2f);
        public static readonly Color IfIconTint     = new(0.35f, 0.78f, 1f);
        public static readonly Color VirtueIconTint = new(0.72f, 0.45f, 0.95f);

        public static string FormatMoney(int money) => $"所持金: {money} 万円";

        public static string FormatIf(int ifScore) => $"IF: {ifScore:F1} pt";

        public static string FormatMental(int mental, int maxMental) =>
            $"メンタル: {mental} / {maxMental}";

        public static string FormatVirtue(int virtue) => $"徳: {virtue} pt";

        /// <summary>screen.md §3.2 — 画面上部に 4 大リソースを全幅で表示。</summary>
        public static Transform SetupTopResourceBar(Transform canvas)
        {
            if (canvas == null) return null;

            var bar = canvas.Find("ResourceBar");
            if (bar == null)
            {
                var go = new GameObject("ResourceBar", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas, false);
                bar = go.transform;
            }

            var bg = bar.GetComponent<Image>() ?? bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.18f, 0.26f, 0.92f);
            bg.raycastTarget = false;
            GameUiChrome.ApplySurface(bar, new Color(0.12f, 0.15f, 0.21f, 0.94f));

            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, TopBarHeight);
            bar.SetAsFirstSibling();

            foreach (var name in new[] { "MoneyText", "IfScoreText", "MentalText", "VirtueText" })
            {
                var t = FindDeep(canvas, name);
                if (t != null) t.SetParent(bar, false);
            }

            EnsureBarStructure(bar);
            StyleBarTexts(bar);
            return bar;
        }

        public static void EnsureBarStructure(Transform bar)
        {
            if (bar == null) return;

            var h = bar.GetComponent<HorizontalLayoutGroup>();
            if (h == null) h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.padding = new RectOffset(12, 12, 6, 6);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            EnsureStatCell(bar, "MoneyCell",  "MoneyText",  "MoneyIcon",  KenneyAssets.ResourceIcon.Money);
            EnsureStatCell(bar, "IfCell",     "IfScoreText", "IfIcon",    KenneyAssets.ResourceIcon.IfScore);
            EnsureStatCell(bar, "MentalCell", "MentalText",  "MentalIcon", KenneyAssets.ResourceIcon.Mental);
            EnsureStatCell(bar, "VirtueCell", "VirtueText",  "VirtueIcon", KenneyAssets.ResourceIcon.Virtue);
        }

        private static void StyleBarTexts(Transform bar)
        {
            foreach (var tmp in bar.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Color c = MoneyTextColor;
                if (tmp.name.Contains("If")) c = IfTextColor;
                else if (tmp.name.Contains("Mental")) c = MentalTextColor;
                else if (tmp.name.Contains("Virtue")) c = VirtueTextColor;
                HudTextStyle.ApplyResource(tmp, c);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        public static void Apply(PlayerData player, Transform canvasOrBar)
        {
            if (player == null || canvasOrBar == null) return;

            var bar = canvasOrBar.name == "ResourceBar"
                ? canvasOrBar
                : canvasOrBar.Find("ResourceBar");
            if (bar == null) return;

            EnsureBarStructure(bar);

            SetText(FindInBar(bar, "MoneyText"),  FormatMoney(player.Money), MoneyTextColor);
            SetText(FindInBar(bar, "IfScoreText"), FormatIf(player.IfScore), IfTextColor);
            SetText(FindInBar(bar, "MentalText"),  FormatMental(player.Mental, player.MaxMental), GetMentalTextColor(player));
            SetText(FindInBar(bar, "VirtueText"),  FormatVirtue(player.Virtue), VirtueTextColor);

            UpdatePinchLabel(FindInBar(bar, "MoneyPinch"),  StatPinchThresholds.IsMoneyPinch(player.Money));
            UpdatePinchLabel(FindInBar(bar, "MentalPinch"), StatPinchThresholds.IsMentalPinch(player.Mental));

            SetIcon(FindInBar(bar, "MoneyIcon"),  KenneyAssets.ResourceIcon.Money,  MoneyIconTint);
            SetIcon(FindInBar(bar, "IfIcon"),     KenneyAssets.ResourceIcon.IfScore, IfIconTint);
            SetIcon(FindInBar(bar, "MentalIcon"), KenneyAssets.ResourceIcon.Mental, GetMentalIconColor(player));
            SetIcon(FindInBar(bar, "VirtueIcon"), KenneyAssets.ResourceIcon.Virtue, VirtueIconTint);
        }

        private static Transform FindInBar(Transform bar, string name)
        {
            var t = bar.Find(name);
            if (t != null) return t;
            foreach (Transform child in bar)
            {
                t = child.Find(name);
                if (t != null) return t;
            }
            return null;
        }

        private static void EnsureStatCell(Transform bar, string cellName, string textName, string iconName,
            KenneyAssets.ResourceIcon iconKind)
        {
            var text = FindInBar(bar, textName);
            if (text == null) return;

            var cell = bar.Find(cellName);
            if (cell == null)
            {
                var cellGo = new GameObject(cellName, typeof(RectTransform));
                cellGo.transform.SetParent(bar, false);
                cell = cellGo.transform;
            }

            var row = cell.GetComponent<HorizontalLayoutGroup>() ?? cell.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            var le = cell.GetComponent<LayoutElement>() ?? cell.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 42f;
            le.preferredHeight = 56f;

            Color accent = iconKind switch
            {
                KenneyAssets.ResourceIcon.Money => MoneyIconTint,
                KenneyAssets.ResourceIcon.IfScore => IfIconTint,
                KenneyAssets.ResourceIcon.Mental => new Color(0.52f, 0.88f, 0.56f),
                KenneyAssets.ResourceIcon.Virtue => VirtueIconTint,
                _ => Color.white
            };
            GameUiChrome.ApplyStatCell(cell, accent);

            if (text.parent != cell)
                text.SetParent(cell, false);

            KenneyUiStyler.EnsureStatIconPublic(cell, iconName, iconKind, Color.white);
            var icon = cell.Find(iconName);
            if (icon != null)
                icon.SetAsFirstSibling();

            var tmp = text.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.Left;
                var textRt = text.GetComponent<RectTransform>();
                textRt.sizeDelta = new Vector2(0f, 42f);
                var textLe = text.GetComponent<LayoutElement>();
                if (textLe == null) textLe = text.gameObject.AddComponent<LayoutElement>();
                textLe.flexibleWidth = 1f;
                textLe.minHeight = 40f;
            }

            var iconRt = cell.Find(iconName)?.GetComponent<RectTransform>();
            if (iconRt != null) iconRt.sizeDelta = new Vector2(32f, 32f);

            if (textName == "MoneyText")
                EnsurePinchLabel(cell, "MoneyPinch");
            else if (textName == "MentalText")
                EnsurePinchLabel(cell, "MentalPinch");
        }

        private static void EnsurePinchLabel(Transform cell, string labelName)
        {
            if (cell.Find(labelName) != null) return;

            var go = new GameObject(labelName, typeof(RectTransform));
            go.transform.SetParent(cell, false);
            go.transform.SetAsLastSibling();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "ピンチ！！";
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            HudTextStyle.ApplyReadable(tmp, HudTextStyle.Scale(16f), PinchTextColor, true);
            HudTextStyle.ApplyOutlineSafe(tmp, 0.12f, new Color(0f, 0f, 0f, 0.75f));

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.preferredWidth = 88f;
            le.minHeight = 40f;

            go.SetActive(false);
        }

        private static void UpdatePinchLabel(Transform label, bool visible)
        {
            if (label == null) return;
            label.gameObject.SetActive(visible);
        }

        public static Color GetMentalIconColor(PlayerData player)
        {
            if (player == null) return new Color(0.45f, 0.95f, 0.5f);
            float ratio = player.MaxMental > 0 ? (float)player.Mental / player.MaxMental : 0f;
            if (ratio <= 0.25f) return new Color(1f, 0.32f, 0.32f);
            if (ratio <= 0.5f)  return new Color(1f, 0.72f, 0.28f);
            return new Color(0.4f, 0.95f, 0.48f);
        }

        public static Color GetMentalTextColor(PlayerData player)
        {
            var icon = GetMentalIconColor(player);
            return new Color(icon.r, icon.g, icon.b, 1f);
        }

        private static void SetText(Transform t, string text, Color color)
        {
            if (t == null) return;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            tmp.text = text;
            HudTextStyle.ApplyResource(tmp, color);
        }

        private static void SetIcon(Transform t, KenneyAssets.ResourceIcon kind, Color tint)
        {
            if (t == null) return;
            var img = t.GetComponent<Image>();
            var sp  = KenneyAssets.GetResourceIcon(kind);
            if (img == null) return;
            if (sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = true;
            }
            img.color = tint;
        }
    }
}
