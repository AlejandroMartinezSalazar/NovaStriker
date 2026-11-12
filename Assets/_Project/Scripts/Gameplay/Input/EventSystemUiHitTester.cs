using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>
    /// Hace un raycast de UI en la posición dada. Funciona igual con ratón y táctil
    /// (a diferencia de IsPointerOverGameObject, cuyo comportamiento depende del id del puntero).
    /// </summary>
    public sealed class EventSystemUiHitTester : IUiHitTester
    {
        private readonly List<RaycastResult> _results = new List<RaycastResult>(8);
        private PointerEventData _eventData;
        private EventSystem _eventDataOwner;

        public bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_eventData == null || _eventDataOwner != eventSystem)
            {
                _eventData = new PointerEventData(eventSystem);
                _eventDataOwner = eventSystem;
            }

            _eventData.position = screenPosition;
            _results.Clear();
            eventSystem.RaycastAll(_eventData, _results);
            return _results.Count > 0;
        }
    }
}
