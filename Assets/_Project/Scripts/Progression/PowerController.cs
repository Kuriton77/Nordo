using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Progression
{
    /// <summary>
    /// A named power grid. When energised it raises <see cref="PowerStateChangedEvent"/> for its grid,
    /// which every <see cref="PoweredDevice"/> on that grid reacts to (lights come on, powered doors
    /// unlock). This is the hub of "restore power → the station wakes up → new areas open".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerController : MonoBehaviour
    {
        [Tooltip("Grid identifier that PoweredDevices match against.")]
        [SerializeField] private string _gridId = "main";

        [Tooltip("Whether this grid is powered at scene start.")]
        [SerializeField] private bool _poweredAtStart;

        private bool _powered;

        /// <summary>The grid this controller drives.</summary>
        public string GridId => _gridId;

        /// <summary>Whether the grid is currently powered.</summary>
        public bool IsPowered => _powered;

        private void Start()
        {
            _powered = false;
            if (_poweredAtStart)
            {
                RestorePower();
            }
            else
            {
                Broadcast(); // establish the initial (unpowered) state for devices
            }
        }

        /// <summary>Energises the grid (idempotent) and announces it.</summary>
        public void RestorePower()
        {
            if (_powered)
            {
                return;
            }

            _powered = true;
            Broadcast();
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent("Power restored. The station hums back to life."));
        }

        /// <summary>Cuts the grid (idempotent) and announces it.</summary>
        public void CutPower()
        {
            if (!_powered)
            {
                return;
            }

            _powered = false;
            Broadcast();
        }

        /// <summary>Sets the grid id at runtime (used by the level builder).</summary>
        public void SetGridId(string gridId) => _gridId = gridId;

        private void Broadcast()
        {
            EventBus<PowerStateChangedEvent>.Raise(new PowerStateChangedEvent(_gridId, _powered));
        }
    }
}
