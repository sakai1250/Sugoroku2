using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>シーン内 UI オブジェクトを名前で検索して参照を解決する。</summary>
    public static class UiBindingUtility
    {
        public static GameObject FindObject(string objectName)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == objectName)
                    return t.gameObject;
            }
            return null;
        }

        public static T FindComponent<T>(string objectName) where T : Component
        {
            var go = FindObject(objectName);
            return go != null ? go.GetComponent<T>() : null;
        }

        public static Transform FindTransform(string objectName)
        {
            var go = FindObject(objectName);
            return go != null ? go.transform : null;
        }
    }
}
