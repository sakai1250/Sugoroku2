using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Network;

namespace Sugoroku.Game
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        private List<EventMaster> _allEvents = new();
        private List<EventMaster> _pool      = new();
        private Dictionary<string, EventMaster> _byId = new();

        public event System.Action<EventMaster, PlayerData> OnEventTriggered;
        public event System.Action<PlayerData, EventChoice> OnChoiceApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadEvents();
        }

        private void LoadEvents()
        {
            var textAsset = Resources.Load<TextAsset>("EventMasters/events");
            if (textAsset == null)
            {
                Debug.LogError("events.json が Resources/EventMasters/ に見つかりません");
                return;
            }

            // JsonUtility は配列直接不可なのでラッパーで解析
            string json = "{\"Events\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<EventMasterList>(json);
            _allEvents = wrapper?.ToList() ?? new List<EventMaster>();
            _byId.Clear();
            foreach (var ev in _allEvents)
            {
                if (ev != null && !string.IsNullOrEmpty(ev.EventId))
                    _byId[ev.EventId] = ev;
            }

            int broken = 0;
            foreach (var ev in _allEvents)
            {
                if (ev == null || ev.ChoiceCount == 0) broken++;
            }
            if (_allEvents.Count == 0)
                Debug.LogError("events.json にイベントが1件も読み込めませんでした。");
            else if (broken > 0)
                Debug.LogWarning($"events.json: 選択肢のないイベントが {broken} 件あります。JSON 形式を確認してください。");
            else
                Debug.Log($"events.json: {_allEvents.Count} 件ロード完了");

            ValidateRobustness();

            RefillPool();
        }

        private void RefillPool()
        {
            _pool = new List<EventMaster>(_allEvents);
            Shuffle(_pool);
        }

        public EventMaster GetById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            return _byId.TryGetValue(eventId, out var ev) ? ev : null;
        }

        public EventMaster DrawEvent() => DrawEventOnAuthority();

        /// <summary>§7.3 — プール抽選（StateAuthority からのみ呼ぶ）。</summary>
        public EventMaster DrawEventOnAuthority()
        {
            if (_pool.Count == 0) RefillPool();
            if (_pool.Count == 0) return null;
            var ev = _pool[0];
            _pool.RemoveAt(0);
            return ev;
        }

        public void TriggerEvent(EventMaster ev, PlayerData player)
        {
            if (ev == null)
            {
                Debug.LogWarning("TriggerEvent: イベントが null です。");
                return;
            }

            var modal = Object.FindFirstObjectByType<Sugoroku.UI.EventModalUI>(FindObjectsInactive.Include);
            modal?.EnsureInitialized();

            bool hasListeners = OnEventTriggered != null;
            if (modal == null)
                Debug.LogError($"イベント「{ev.Title}」: UI が未接続です。EventModalUI を確認してください。");

            OnEventTriggered?.Invoke(ev, player);

            if (!player.IsCpu)
            {
                if (modal != null)
                    modal.ShowEventFromManager(ev, player);
                else if (!hasListeners)
                    Sugoroku.UI.EventModalUI.ShowEventDirect(ev, player);
            }
        }

        public void ApplyChoice(PlayerData player, EventChoice choice)
        {
            // キャラ固有の補正
            int money   = choice.MoneyChange;
            int ifScore = choice.IfScoreChange;
            int mental  = choice.MentalChange;
            int virtue  = choice.VirtueChange;

            player.ApplyStatChange(money, ifScore, mental, virtue);
            OnChoiceApplied?.Invoke(player, choice);
        }

        private void ValidateRobustness()
        {
            var issues = EventRobustnessValidator.ValidateAll(_allEvents);
            string report = EventRobustnessValidator.FormatReport(issues);
            if (issues.Count > 0)
                Debug.LogError(report);
            else
                Debug.Log(report);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = GameRng.Range(0, i);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

    }
}
