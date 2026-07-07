using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.UI
{
    /// <summary>
    /// 全画面共通の UI 描画順。個別オブジェクトの位置調整ではなく、Canvas / sibling 規則を一元管理する。
    /// </summary>
    public static class UiLayerManager
    {
        public const int SortBoardHud      = 0;
        public const int SortStatusBanner  = 10;
        public const int SortEventModal    = 40;
        public const int SortToastOverlay  = 50;

        public const string EventModalRootName = "EventModalOverlayRoot";

        public static void ConfigureCanvas(Canvas canvas, int sortingOrder)
        {
            if (canvas == null) return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        /// <summary>常時 HUD 直下にバナーを置く（モーダルより背面）。</summary>
        public static void PlaceStatusBanner(Transform bannerRoot, Transform hudCanvas)
        {
            if (bannerRoot == null || hudCanvas == null) return;
            bannerRoot.SetParent(hudCanvas, false);
            bannerRoot.SetAsLastSibling();
        }

        /// <summary>イベントモーダル専用ルート。HUD・バナーより常に手前。</summary>
        public static Transform EnsureEventModalRoot(Canvas hudCanvas)
        {
            if (hudCanvas == null) return null;

            var existing = hudCanvas.transform.Find(EventModalRootName);
            Transform root;
            if (existing != null)
            {
                root = existing;
            }
            else
            {
                var go = new GameObject(EventModalRootName, typeof(RectTransform));
                go.transform.SetParent(hudCanvas.transform, false);
                Stretch(go.GetComponent<RectTransform>());
                root = go.transform;
            }

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortEventModal;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.gameObject.AddComponent<GraphicRaycaster>();

            root.SetAsLastSibling();
            return root;
        }

        public static void ApplyEventModalOpen()
        {
            GameStatusBanner.SetSuppressed(true);
            GameplayUiOverlayQueue.ClearPending();
        }

        public static void ApplyEventModalClosed()
        {
            GameStatusBanner.SetSuppressed(false);
        }

        public static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
