using UnityEngine;
using UnityEngine.EventSystems;

namespace Sugoroku.UI
{
    /// <summary>カードホバー時に §2.4 の軽いフィードバック。</summary>
    public class CharacterSelectCardHover : MonoBehaviour, IPointerEnterHandler
    {
        private CharacterSelectController _host;
        private int _index;

        public void Setup(CharacterSelectController host, int index)
        {
            _host  = host;
            _index = index;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            _host?.OnCardHover(_index);
    }
}
