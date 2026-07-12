using UnityEngine;
using Nordo.Interaction;

namespace Nordo.Progression
{
    /// <summary>
    /// A lock that releases when its grid is powered — the electromagnetic bolt on the way out. While
    /// unpowered the door stays locked (and rattles when tried); restoring power unlocks it, opening a
    /// new area. This is how "restore power → new areas unlock" is expressed mechanically.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoweredLock : PoweredDevice
    {
        [Tooltip("The lock released when power is restored.")]
        [SerializeField] private Lockable _lockable;

        [Tooltip("Re-lock when power is lost.")]
        [SerializeField] private bool _relockOnPowerLoss;

        private void Awake()
        {
            if (_lockable == null)
            {
                _lockable = GetComponent<Lockable>();
            }
        }

        protected override void OnPowerChanged(bool powered)
        {
            if (_lockable == null)
            {
                return;
            }

            if (powered)
            {
                _lockable.Unlock();
            }
            else if (_relockOnPowerLoss)
            {
                _lockable.Lock();
            }
        }

        /// <summary>Assigns the controlled lock at runtime (used by the level builder).</summary>
        public void SetLock(Lockable lockable) => _lockable = lockable;
    }
}
