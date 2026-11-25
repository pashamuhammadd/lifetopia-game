using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lifetopia.Controls
{
    /// <summary>
    /// On-screen button that fires on press (not release) for responsive touch play.
    /// Works with touch and mouse through the EventSystem.
    /// </summary>
    public sealed class TouchActionButton : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;

        public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();
    }
}
