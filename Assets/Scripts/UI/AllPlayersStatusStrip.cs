using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>
    /// 全プレイヤーのリソースを常に一覧できるミニパネル。
    /// 自分のターン以外でも「他の人は今いくら/何位か」を確認でき、脱落者も見える。
    /// </summary>
    [DisallowMultipleComponent]
    public class AllPlayersStatusStrip : MonoBehaviour
    {
        private RectTransform _root;
        private readonly List<(GameObject row, TextMeshProUGUI label)> _rows = new();

        private System.Action<PlayerData> _turnStartedHandler;
        private System.Action<PlayerData, string> _squareEffectHandler;
        private System.Action<string> _logHandler;
        private bool _subscribedToGameManager;

        private void OnEnable()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
            SubscribeToGameManager();
            Refresh();
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
            UnsubscribeFromGameManager();
        }

        private void HandleTurnStarted(PlayerData _) => Refresh();

        /// <summary>イベント適用・ログ出力のたびに全員状況を更新し、常に最新の情報を表示する。</summary>
        private void SubscribeToGameManager()
        {
            if (_subscribedToGameManager) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            _squareEffectHandler ??= (_, _) => Refresh();
            _logHandler          ??= _   => Refresh();
            gm.OnSquareEffect    += _squareEffectHandler;
            gm.OnLog             += _logHandler;
            _subscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null || !_subscribedToGameManager) return;
            if (_squareEffectHandler != null) gm.OnSquareEffect -= _squareEffectHandler;
            if (_logHandler != null)          gm.OnLog          -= _logHandler;
            _subscribedToGameManager = false;
        }

        public void Refresh()
        {
            EnsurePanel();
            SubscribeToGameManager();
            if (_root == null) return;

            var players = GameManager.Instance != null && GameManager.Instance.IsInitialized
                ? GameManager.Instance.GetAllPlayers()
                : System.Array.Empty<PlayerData>();
            if (players.Length == 0) return;

            int current = GameManager.Instance.GetCurrentPlayerIndex();

            EnsureRowCount(players.Length);

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (i >= _rows.Count || _rows[i].label == null) continue;

                string marker = i == current ? "▶ " : "";
                string state = p.Status switch
                {
                    PlayerStatus.Graduated => $" [修了{p.FinishRank}位]",
                    PlayerStatus.Dropout   => " [脱落]",
                    _                      => ""
                };

                _rows[i].label.text =
                    $"{marker}{PlayerIdentity.FormatHudLabel(p)}{state}  " +
                    $"{p.Money}万  IF{p.IfScore}  メンタル{p.Mental}  徳{p.Virtue}";
                _rows[i].label.color = i == current ? Color.white : new Color(0.86f, 0.89f, 0.95f, 0.95f);

                var bg = _rows[i].row.GetComponent<Image>();
                if (bg != null)
                    bg.color = i == current
                        ? new Color(0.16f, 0.19f, 0.27f, 0.88f)
                        : new Color(0.10f, 0.12f, 0.18f, 0.66f);
            }
        }

        private void EnsurePanel()
        {
            if (_root != null) return;

            var existing = transform.Find("AllPlayersStatusStrip");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("AllPlayersStatusStrip", typeof(RectTransform));
                go.transform.SetParent(transform, false);
            }

            _root = go.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(1f, 1f);
            _root.anchorMax = new Vector2(1f, 1f);
            _root.pivot = new Vector2(1f, 1f);
            _root.anchoredPosition = new Vector2(-UiSafeLayout.OuterMargin,
                -(ResourceHudVisuals.TopBarHeight + 10f));
            _root.sizeDelta = new Vector2(520f, 30f);
        }

        private void EnsureRowCount(int count)
        {
            while (_rows.Count > count)
            {
                var last = _rows[^1];
                if (last.row != null) Destroy(last.row);
                _rows.RemoveAt(_rows.Count - 1);
            }

            float rowH = HudTextStyle.Scale(24f);
            for (int i = _rows.Count; i < count; i++)
            {
                var rowGo = new GameObject($"PlayerRow_{i}", typeof(RectTransform), typeof(Image));
                rowGo.transform.SetParent(_root, false);
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, -rowH * i - 2f);
                rowRt.sizeDelta = new Vector2(0f, rowH - 3f);
                var rowImg = rowGo.GetComponent<Image>();
                rowImg.color = new Color(0.10f, 0.12f, 0.18f, 0.66f);
                rowImg.raycastTarget = false;

                var tmpGo = new GameObject("Label", typeof(RectTransform));
                tmpGo.transform.SetParent(rowGo.transform, false);
                var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.raycastTarget = false;
                HudTextStyle.ApplyReadable(tmp, HudTextStyle.Scale(13f), Color.white, bold: true);
                var labelRt = tmp.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(10f, 0f);
                labelRt.offsetMax = new Vector2(-10f, 0f);

                _rows.Add((rowGo, tmp));
            }

            _root.sizeDelta = new Vector2(_root.sizeDelta.x, rowH * count + 4f);
        }
    }
}
