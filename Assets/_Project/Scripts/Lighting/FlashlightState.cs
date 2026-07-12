using System;

namespace Nordo.Lighting
{
    /// <summary>
    /// The serializable snapshot of a flashlight: its remaining charge and whether it is switched on.
    /// Captured/restored via <see cref="ISaveable{TState}"/> so the flashlight persists across saves
    /// (the save service in Milestone 9 will collect these).
    /// </summary>
    [Serializable]
    public struct FlashlightState
    {
        /// <summary>Remaining battery charge in units.</summary>
        public float Charge;

        /// <summary>Whether the flashlight was switched on.</summary>
        public bool IsOn;

        public FlashlightState(float charge, bool isOn)
        {
            Charge = charge;
            IsOn = isOn;
        }
    }
}
