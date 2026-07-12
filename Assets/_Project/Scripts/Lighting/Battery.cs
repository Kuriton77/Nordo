using System;
using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// A pure, serializable battery model — capacity, current charge, and the operations to drain and
    /// recharge it — deliberately separated from the flashlight (Single Responsibility). It has no
    /// Unity dependencies beyond serialization, which makes it trivially unit-testable and reusable
    /// for anything else that runs on batteries (a handheld radio, a portable lamp).
    /// <para>
    /// Charge is expressed in abstract "units"; the flashlight decides how fast it drains and how much
    /// a pickup restores, so the same battery drives very different devices.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class Battery
    {
        [Tooltip("Maximum charge the battery can hold.")]
        [Min(0.01f)]
        [SerializeField] private float _capacity = 100f;

        [Tooltip("Charge present at scene start (clamped to capacity). Ignored if 'Start Full' is on.")]
        [Min(0f)]
        [SerializeField] private float _initialCharge = 100f;

        [Tooltip("Begin fully charged regardless of Initial Charge.")]
        [SerializeField] private bool _startFull = true;

        private float _charge;
        private bool _initialized;

        /// <summary>Raised whenever the charge changes, passing the new 0–1 fraction.</summary>
        public event Action<float> ChargeChanged;

        /// <summary>Raised the moment charge reaches zero from a positive value.</summary>
        public event Action Depleted;

        /// <summary>Maximum charge.</summary>
        public float Capacity => _capacity;

        /// <summary>Current charge in units.</summary>
        public float Charge => _charge;

        /// <summary>Current charge as a 0–1 fraction of capacity.</summary>
        public float Fraction => _capacity > 0f ? Mathf.Clamp01(_charge / _capacity) : 0f;

        /// <summary>True when no charge remains.</summary>
        public bool IsEmpty => _charge <= 0f;

        /// <summary>Parameterless constructor for Unity serialization / inspector authoring.</summary>
        public Battery()
        {
        }

        /// <summary>Constructs a battery directly (used by tests and runtime spawns).</summary>
        public Battery(float capacity, float initialCharge, bool startFull = false)
        {
            _capacity = Mathf.Max(0.01f, capacity);
            _initialCharge = Mathf.Clamp(initialCharge, 0f, _capacity);
            _startFull = startFull;
            Initialize();
        }

        /// <summary>Sets the starting charge. Call once before use (the flashlight calls this in Awake).</summary>
        public void Initialize()
        {
            _charge = _startFull ? _capacity : Mathf.Clamp(_initialCharge, 0f, _capacity);
            _initialized = true;
        }

        /// <summary>
        /// Removes <paramref name="amount"/> units of charge, clamped at zero, raising change/deplete
        /// events. No-op for non-positive amounts. Cheap and allocation-free.
        /// </summary>
        public void Drain(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f || _charge <= 0f)
            {
                return;
            }

            bool wasPositive = _charge > 0f;
            _charge = Mathf.Max(0f, _charge - amount);
            ChargeChanged?.Invoke(Fraction);

            if (wasPositive && _charge <= 0f)
            {
                Depleted?.Invoke();
            }
        }

        /// <summary>Adds <paramref name="amount"/> units of charge, clamped at capacity.</summary>
        public void Recharge(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f)
            {
                return;
            }

            _charge = Mathf.Min(_capacity, _charge + amount);
            ChargeChanged?.Invoke(Fraction);
        }

        /// <summary>Directly sets the charge (used by save/load restore).</summary>
        public void SetCharge(float charge)
        {
            EnsureInitialized();
            _charge = Mathf.Clamp(charge, 0f, _capacity);
            ChargeChanged?.Invoke(Fraction);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }
    }
}
