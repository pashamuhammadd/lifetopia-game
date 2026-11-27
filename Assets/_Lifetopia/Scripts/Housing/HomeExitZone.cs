using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>Trigger just outside the door opening; walking into it leaves the house.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HomeExitZone : MonoBehaviour
    {
        private HouseInteriorController _owner;

        public void Bind(HouseInteriorController owner) => _owner = owner;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_owner != null) _owner.OnPlayerReachedExit(other);
        }
    }
}
