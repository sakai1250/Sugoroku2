using System.IO;
using UnityEditor;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Editor
{
    public static class EventRobustnessValidatorMenu
    {
        private const string EventsPath = "Assets/Resources/EventMasters/events.json";

        [MenuItem("Tools/Sugoroku/Validate events.json (§7.1)")]
        public static void ValidateFromMenu()
        {
            string fullPath = Path.Combine(Application.dataPath, "Resources/EventMasters/events.json");
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"events.json が見つかりません: {EventsPath}");
                return;
            }

            string json = File.ReadAllText(fullPath);
            string wrapped = "{\"Events\":" + json + "}";
            var wrapper = JsonUtility.FromJson<EventMasterList>(wrapped);
            var list = wrapper?.ToList() ?? new System.Collections.Generic.List<EventMaster>();

            var issues = EventRobustnessValidator.ValidateAll(list);
            string report = EventRobustnessValidator.FormatReport(issues);
            if (issues.Count > 0)
                Debug.LogError(report);
            else
                Debug.Log(report);
        }
    }
}
