using System;
using UnityEngine;

namespace Nordo.Level
{
    /// <summary>
    /// The trigger the player crosses to finish the section (the transmitter room). Fires
    /// <see cref="Reached"/> once; the director completes the final objective and shows the ending.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class SectionExit : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";
        private bool _triggered;

        /// <summary>Raised once when the player reaches the exit.</summary>
        public event Action Reached;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

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
