using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>初回チュートリアル: 人間プレイヤーの最初の3ターンだけ、4リソースの意味を吹き出しで説明する。</summary>
    public class TutorialTooltipController : MonoBehaviour
    {
        private const string PrefsKey = "tutorial_resources_seen";
        private const int    MaxTurnsToShow = 4;

        private static readonly string[] Tips =
        {
            "「所持金」: 研究費・生活費。0になると破産で退学してしまいます。",
            "「IF」: インパクトファクター。研究の実績値で、スコアへの影響が最も大きい指標です。",
            "「メンタル」: 心の余裕。0になると力尽きて失踪してしまいます。",
            "「徳」: 日頃の善行ポイント。ピンチの時に救済してくれることがあります。",
        };

        private CanvasGroup _group;
        private TextMeshProUGUI _label;
        private int _humanTurnsSeen;
        private bool _subscribed;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            if (PlayerPrefs.GetInt(PrefsKey, 0) != 0)
            {
                enabled = false;
                return;
            }
            BuildUi();
        }

        private void OnEnable()
        {
            if (!_subscribed) StartCoroutine(SubscribeWhenReady());
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
        }

        private System.Collections.IEnumerator SubscribeWhenReady()
        {
            while (TurnManager.Instance == null || GameManager.Instance == null)
                yield return null;

            TurnManager.Instance.OnTurnStarted += OnTurnStarted;
            _subscribed = true;
        }

        private void OnTurnStarted(PlayerData player)
        {
            if (player == null || player.IsCpu) return;
            if (_humanTurnsSeen >= MaxTurnsToShow)
            {
                PlayerPrefs.SetInt(PrefsKey, 1);
                PlayerPrefs.Save();
                if (TurnManager.Instance != null)
                    TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
                enabled = false;
                return;
            }

            string tip = Tips[Mathf.Clamp(_humanTurnsSeen, 0, Tips.Length - 1)];
            _humanTurnsSeen++;
            ShowTip(tip);
        }

        private void ShowTip(string text)
        {
            if (_label == null) return;
            // 絵文字(💡)は NotoSansJP に存在しないため代替記号を使用する。
            _label.text = $"★ {text}";
            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(ShowThenHide());
        }

        private System.Collections.IEnumerator ShowThenHide()
        {
            if (_group != null) _group.alpha = 1f;

            // 読む速度に合わせられるよう、クリックかキー入力で閉じるまで表示する（最長15秒）。
            float elapsed = 0f;
            while (elapsed < 15f)
            {
                if (WasDismissInput()) break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float t = 0f;
            const float fade = 0.6f;
            float start = _group != null ? _group.alpha : 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                if (_group != null) _group.alpha = Mathf.Lerp(start, 0f, t / fade);
                yield return null;
            }
            if (_group != null) _group.alpha = 0f;
        }

        private static bool WasDismissInput()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return false;
            foreach (var key in keyboard.allKeys)
                if (key != null && key.wasPressedThisFrame) return true;
            return false;
        }

        private void BuildUi()
        {
            var root = new GameObject("TutorialTooltip", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(ResourceHudVisuals.TopBarHeight + 90f));
            rt.sizeDelta = new Vector2(760f, 64f);

            GameUiChrome.ApplySurface(root.transform, new Color(0.16f, 0.13f, 0.05f, 0.92f));
            GameUiChrome.ApplyAccentRail(root.transform, new Color(1f, 0.82f, 0.30f, 0.9f), 4f);

            _group = root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            // 情報表示専用のため下のUI操作を妨げない
            _group.blocksRaycasts = false;
            _group.interactable = false;
            var surfaceImg = root.GetComponent<Image>();
            if (surfaceImg != null) surfaceImg.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 4f);
            textRt.offsetMax = new Vector2(-16f, -4f);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.fontSize = 22f;
            HudTextStyle.ApplyReadable(_label, 22f, new Color(1f, 0.96f, 0.85f), false);
            JapaneseFontProvider.Apply(_label);
        }
    }
}
