using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>screen.md §5.2 — 3大ゲームオーバー画面のUI演出。</summary>
    public class GameOverSceneJuice : MonoBehaviour
    {
        [SerializeField] private RectTransform _stage;
        [SerializeField] private Image         _accentPanel;

        private Coroutine _routine;

        public void Play(GameOverOutcome outcome)
        {
            if (_routine != null) StopCoroutine(_routine);
            if (_accentPanel != null) _accentPanel.gameObject.SetActive(false);
            EnsureStage();
            _routine = StartCoroutine(PlayRoutine(outcome));
        }

        private void EnsureStage()
        {
            if (_stage != null) return;
            var canvas = GetComponentInParent<Canvas>();
            var parent = transform as RectTransform;
            if (parent == null) return;

            var go = new GameObject("GameOverVisualStage", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _stage = go.GetComponent<RectTransform>();
            _stage.anchorMin = Vector2.zero;
            _stage.anchorMax = Vector2.one;
            _stage.offsetMin = _stage.offsetMax = Vector2.zero;
            _stage.SetAsLastSibling();
        }

        private IEnumerator PlayRoutine(GameOverOutcome outcome)
        {
            ClearStage();
            switch (outcome.VisualStyle)
            {
                case GameOverVisualStyle.BankruptcyNotice:
                    yield return BankruptcyNoticeRoutine(outcome.AccentColor);
                    break;
                case GameOverVisualStyle.MissingPhone:
                    yield return MissingPhoneRoutine(outcome.AccentColor);
                    break;
                case GameOverVisualStyle.ExpulsionList:
                    yield return ExpulsionListRoutine(outcome.AccentColor);
                    break;
            }
        }

        private void ClearStage()
        {
            if (_stage == null) return;
            for (int i = _stage.childCount - 1; i >= 0; i--)
                Destroy(_stage.GetChild(i).gameObject);
        }

        private IEnumerator BankruptcyNoticeRoutine(Color accent)
        {
            var doc = CreatePanel(_stage, "Notice", new Vector2(420f, 520f), accent);
            var rt = doc.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 80f);
            rt.localScale = Vector3.one * 2.8f;
            rt.localRotation = Quaternion.Euler(0f, 0f, 8f);

            var title = CreateLabel(doc.transform, "強制退学\n通知書", 36, Color.white, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            Stretch(title.rectTransform);

            var sub = CreateLabel(doc.transform, "学費・生活費未納", 18, new Color(1f, 0.9f, 0.9f));
            sub.alignment = TextAlignmentOptions.Center;
            var subRt = sub.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 24f);
            subRt.sizeDelta = new Vector2(360f, 40f);

            float dur = GameConfig.AnimationDuration(0.45f);
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rt.localScale = Vector3.one * Mathf.Lerp(2.8f, 1f, eased);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(8f, -2f, eased));
                rt.anchoredPosition = Vector2.Lerp(new Vector2(0f, 200f), new Vector2(0f, 80f), eased);
                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                rt.anchoredPosition += new Vector2(Random.Range(-6f, 6f), Random.Range(-4f, 4f));
                yield return new WaitForSeconds(GameConfig.AnimationDuration(0.04f));
            }
            rt.anchoredPosition = new Vector2(0f, 80f);
        }

        private IEnumerator MissingPhoneRoutine(Color glow)
        {
            var room = CreatePanel(_stage, "DarkRoom", new Vector2(900f, 600f), new Color(0.04f, 0.04f, 0.06f, 0.85f));
            var roomRt = room.GetComponent<RectTransform>();
            roomRt.anchoredPosition = new Vector2(0f, 40f);

            var phone = CreatePanel(room.transform, "Phone", new Vector2(200f, 360f), new Color(0.08f, 0.08f, 0.1f));
            var phoneRt = phone.GetComponent<RectTransform>();
            phoneRt.anchoredPosition = new Vector2(0f, -20f);

            var screen = CreatePanel(phone.transform, "Screen", new Vector2(176f, 300f), new Color(0.12f, 0.14f, 0.22f));
            Stretch(screen.GetComponent<RectTransform>(), 8f);

            var glowImg = screen.AddComponent<Outline>();
            glowImg.effectColor = glow;
            glowImg.effectDistance = new Vector2(2f, -2f);

            string[] msgs =
            {
                "教授 ○○: 至急連絡ください",
                "研究室: 論文の提出は？",
                "未読 47件",
                "着信拒否中…",
                "ゼミ: 出席確認",
            };

            float y = 120f;
            foreach (var msg in msgs)
            {
                var line = CreateLabel(screen.transform, msg, 13, new Color(0.7f, 0.75f, 0.9f));
                line.alignment = TextAlignmentOptions.Left;
                var lrt = line.rectTransform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(12f, y);
                lrt.sizeDelta = new Vector2(150f, 24f);
                y -= 28f;
            }

            var stamp = CreateLabel(room.transform, "消息不明", 42, glow, FontStyles.Bold);
            stamp.alignment = TextAlignmentOptions.Center;
            var stampRt = stamp.rectTransform;
            stampRt.anchoredPosition = new Vector2(0f, -200f);
            stampRt.sizeDelta = new Vector2(400f, 60f);

            float pulse = 0f;
            float pulseDuration = GameConfig.AnimationDuration(4f);
            while (pulse < pulseDuration)
            {
                pulse += Time.deltaTime;
                float phase = pulse / GameConfig.AnimationDurationScale;
                float a = 0.65f + Mathf.Sin(phase * 3f) * 0.15f;
                screen.GetComponent<Image>().color = new Color(0.12f * a, 0.14f * a, 0.28f, 1f);
                yield return null;
            }
        }

        private IEnumerator ExpulsionListRoutine(Color accent)
        {
            var board = CreatePanel(_stage, "Bulletin", new Vector2(480f, 360f), new Color(0.92f, 0.88f, 0.75f));
            var boardRt = board.GetComponent<RectTransform>();
            boardRt.anchoredPosition = new Vector2(0f, 80f);

            var header = CreateLabel(board.transform, "除籍対象者一覧", 28, new Color(0.25f, 0.15f, 0.1f), FontStyles.Bold);
            header.alignment = TextAlignmentOptions.Center;
            var hRt = header.rectTransform;
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -16f);
            hRt.sizeDelta = new Vector2(0f, 40f);

            string highlightLine = GetHighlightedStudentLine();
            string[] ids = { "学籍 No. 2021041", "学籍 No. 2021088", highlightLine, "学籍 No. 2021156" };
            float y = -70f;
            for (int i = 0; i < ids.Length; i++)
            {
                bool isHighlighted = ids[i].Contains("▶");
                var line = CreateLabel(board.transform, ids[i], isHighlighted ? 22 : 16,
                    isHighlighted ? accent : new Color(0.35f, 0.3f, 0.25f),
                    isHighlighted ? FontStyles.Bold : FontStyles.Normal);
                line.alignment = TextAlignmentOptions.Left;
                var lrt = line.rectTransform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(32f, y);
                lrt.sizeDelta = new Vector2(400f, 32f);
                y -= isHighlighted ? 44f : 32f;
            }

            boardRt.localScale = Vector3.one * 0.85f;
            float elapsed = 0f;
            float duration = GameConfig.AnimationDuration(0.35f);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                boardRt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, elapsed / duration);
                yield return null;
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return go;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            JapaneseFontProvider.Apply(tmp);
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static string GetHighlightedStudentLine()
        {
            var players = GameSession.LastPlayers;
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (!p.IsCpu)
                        return $"{p.Name}（学籍番号）▶";
                }
            }
            return "学籍 No. 2021123 ▶";
        }

        private static void Stretch(RectTransform rt, float margin = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
