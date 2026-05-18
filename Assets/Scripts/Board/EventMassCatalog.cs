using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>盤面マス用に events.json を参照するカタログ。</summary>
    public static class EventMassCatalog
    {
        private static Dictionary<string, EventMaster> _byId;
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            _byId = new Dictionary<string, EventMaster>();
            var textAsset = Resources.Load<TextAsset>("EventMasters/events");
            if (textAsset == null)
            {
                _loaded = true;
                return;
            }

            string json = "{\"Events\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<EventMasterList>(json);
            if (wrapper?.Events == null)
            {
                _loaded = true;
                return;
            }

            foreach (var ev in wrapper.Events)
            {
                if (ev == null || string.IsNullOrEmpty(ev.EventId)) continue;
                _byId[ev.EventId] = ev;
            }

            _loaded = true;
        }

        public static EventMaster Get(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            EnsureLoaded();
            return _byId != null && _byId.TryGetValue(eventId, out var ev) ? ev : null;
        }

        public static void ClearCache()
        {
            _byId?.Clear();
            _loaded = false;
        }
    }
}
