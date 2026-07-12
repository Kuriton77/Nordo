using System;
using UnityEngine;

namespace Nordo.VerticalSlice
{
    /// <summary>
    /// A trigger volume that fires <see cref="Reached"/> when the tagged player enters — the escape
    /// point of the test area. Kept trivially small: the director decides what "escaped" means.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class ExitZone : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";

        private bool _triggered;

        /// <summary>Raised once when the player first reaches the zone.</summary>
        public event Action Reached;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered || !other.CompareTag(_playerTag))
            {
                return;
            }

            _triggered = true;
            Reached?.Invoke();
        }
    }
}
