using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Progression
{
    /// <summary>
    /// Base for anything that reacts to a power grid — lights, powered locks, machinery. Subscribes to
    /// <see cref="PowerStateChangedEvent"/>, filters by grid id, and calls <see cref="OnPowerChanged"/>.
    /// Devices begin unpowered, so a section starts dark and locked and comes alive when the generator
    /// runs — the whole point of the progression.
    /// </summary>
    public abstract class PoweredDevice : MonoBehaviour
    {
        [Tooltip("Power grid this device belongs to (must match a PowerController's grid id).")]
        [SerializeField] protected string _gridId = "main";

        /// <summary>Whether this device currently has power.</summary>
        protected bool IsPowered { get; private set; }

        protected virtual void OnEnable()
        {
            EventBus<PowerStateChangedEvent>.Subscribe(OnPowerEvent);
        }

        protected virtual void OnDisable()
        {
            EventBus<PowerStateChangedEvent>.Unsubscribe(OnPowerEvent);
        }

        protected virtual void Start()
        {
            // Establish the visual/logic default (unpowered) at spawn.
            OnPowerChanged(false);
        }

        /// <summary>Sets the grid id at runtime (used by the level builder).</summary>
        public void SetGridId(string gridId) => _gridId = gridId;

        private void OnPowerEvent(PowerStateChangedEvent evt)
        {
            if (evt.GridId != _gridId)
            {
                return;
            }

            IsPowered = evt.Powered;
            OnPowerChanged(evt.Powered);
        }

        /// <summary>Called whenever this device's grid power changes.</summary>
        protected abstract void OnPowerChanged(bool powered);
    }
}
