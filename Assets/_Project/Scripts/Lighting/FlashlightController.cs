using System;
using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Input;

namespace Nordo.Lighting
{
    /// <summary>
    /// The flashlight: toggling, battery drain/recharge, flicker modulation, audio, and the light
    /// itself. It orchestrates the small, focused pieces built alongside it — a <see cref="Battery"/>,
    /// a stack of <see cref="ILightModulator"/>s, a <see cref="FlashlightSettings"/> asset and a
    /// <see cref="LightQualityPreset"/> — rather than doing everything itself (Single Responsibility,
    /// composition).
    /// <para>
    /// It also implements <see cref="ILightSource"/> (so the future enemy AI can query whether a point
    /// is lit) and <see cref="ISaveable{TState}"/> (so its charge/on-state persist). Hot paths avoid
    /// allocations: modulators are cached once and iterated by index, and events are only raised on
    /// genuine state changes.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlashlightController : MonoBehaviour, ILightSource, ISaveable<FlashlightState>
    {
        [Header("References")]
        [Tooltip("The spotlight to drive. If empty, a Light in this object's children is used.")]
        [SerializeField] private Light _light;

        [Tooltip("Shared input asset (provides the Flashlight toggle action).")]
        [SerializeField] private InputReader _input;

        [Tooltip("Beam + power tuning.")]
        [SerializeField] private FlashlightSettings _settings;

        [Tooltip("Optional shadow/render quality preset applied to the light on start.")]
        [SerializeField] private LightQualityPreset _qualityPreset;

        [Header("Battery")]
        [SerializeField] private Battery _battery = new Battery();

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _toggleOnClip;
        [SerializeField] private AudioClip _toggleOffClip;
        [SerializeField] private AudioClip _depletedClip;
        [SerializeField] private AudioClip _rechargeClip;
        [SerializeField] private AudioClip _lowWarningClip;

        [Header("Light Visibility (AI hook)")]
        [Tooltip("Layers that block the beam for illumination queries (walls). Excludes the player.")]
        [SerializeField] private LayerMask _occluderMask = ~0;

        [Header("Save")]
        [Tooltip("Stable id used to match this flashlight's saved state.")]
        [SerializeField] private string _saveId = "player_flashlight";

        // --- Public state / events --------------------------------------------------

        /// <summary>Whether the flashlight is currently switched on (and has power).</summary>
        public bool IsOn { get; private set; }

        /// <summary>Battery charge as a 0–1 fraction, for indicators/UI.</summary>
        public float BatteryFraction => _battery.Fraction;

        /// <summary>Raised when the on/off state changes.</summary>
        public event Action<bool> StateChanged;

        // --- Internals --------------------------------------------------------------

        private readonly List<ILightModulator> _modulators = new();
        private ILightVisibilityService _visibilityService;
        private float _timeOn;
        private float _lastWarningTime;
        private float _currentMultiplier = 1f; // last frame's combined flicker multiplier

        private void Awake()
        {
            if (_light == null)
            {
                _light = GetComponentInChildren<Light>(true);
            }

            GetComponents(_modulators);
            _battery.Initialize();

            if (_light != null)
            {
                _light.type = LightType.Spot;
                ApplySettingsToLight();
                _qualityPreset?.Apply(_light);
                _light.enabled = false;
            }

            _battery.Depleted += OnBatteryDepleted;
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.FlashlightToggled += OnToggleInput;
            }

            EventBus<BatteryCollectedEvent>.Subscribe(OnBatteryCollected);

            if (ServiceLocator.TryGet(out _visibilityService) && _visibilityService != null)
            {
                _visibilityService.RegisterSource(this);
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.FlashlightToggled -= OnToggleInput;
            }

            EventBus<BatteryCollectedEvent>.Unsubscribe(OnBatteryCollected);

            _visibilityService?.UnregisterSource(this);
            _visibilityService = null;
        }

        private void OnDestroy()
        {
            _battery.Depleted -= OnBatteryDepleted;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (IsOn)
            {
                _timeOn += dt;
                _battery.Drain(_settings != null ? _settings.DrainPerSecond * dt : dt);
                UpdateLowBatteryWarning();
            }

            UpdateBeam(dt);
        }

        /// <summary>Toggles the flashlight (the bound input calls this).</summary>
        public void Toggle() => SetOn(!IsOn);

        /// <summary>Switches the flashlight on or off, with audio and event feedback.</summary>
        public void SetOn(bool on)
        {
            if (on == IsOn)
            {
                return;
            }

            // Can't turn on a dead battery — give the dry "click" instead.
            if (on && _battery.IsEmpty)
            {
                PlayClip(_depletedClip);
                return;
            }

            IsOn = on;
            _timeOn = 0f;

            if (_light != null)
            {
                _light.enabled = on;
            }

            PlayClip(on ? _toggleOnClip : _toggleOffClip);
            StateChanged?.Invoke(on);
        }

        // --- Beam / modulation ------------------------------------------------------

        private void UpdateBeam(float dt)
        {
            if (_light == null)
            {
                return;
            }

            if (!IsOn)
            {
                _currentMultiplier = 0f;
                return;
            }

            var state = new LightModulatorState(IsOn, _battery.Fraction, _timeOn);

            // Combine all active modulators multiplicatively (no allocation).
            float multiplier = 1f;
            for (int i = 0; i < _modulators.Count; i++)
            {
                ILightModulator modulator = _modulators[i];
                if (modulator != null && modulator.IsActive)
                {
                    multiplier *= Mathf.Clamp01(modulator.Evaluate(dt, in state));
                }
            }

            _currentMultiplier = multiplier;
            float baseIntensity = _settings != null ? _settings.Intensity : 4.5f;
            _light.intensity = baseIntensity * multiplier;
        }

        private void ApplySettingsToLight()
        {
            if (_settings == null || _light == null)
            {
                return;
            }

            _light.intensity = _settings.Intensity;
            _light.range = _settings.Range;
            _light.spotAngle = _settings.SpotAngle;
            _light.innerSpotAngle = Mathf.Min(_settings.InnerSpotAngle, _settings.SpotAngle);
            _light.color = _settings.Color;
        }

        /// <summary>Sets the beam intensity at runtime (e.g. an upgrade or a settings slider).</summary>
        public void SetIntensity(float intensity)
        {
            if (_settings != null)
            {
                _settings.Intensity = Mathf.Max(0f, intensity);
            }
        }

        /// <summary>Sets the outer beam angle at runtime (e.g. a focus mechanic).</summary>
        public void SetBeamAngle(float spotAngle)
        {
            if (_light != null)
            {
                _light.spotAngle = Mathf.Clamp(spotAngle, 1f, 179f);
            }
        }

        private void UpdateLowBatteryWarning()
        {
            if (_settings == null || _lowWarningClip == null)
            {
                return;
            }

            if (_battery.Fraction <= _settings.LowBatteryThreshold && _battery.Fraction > 0f
                && Time.time - _lastWarningTime >= _settings.WarningBeepInterval)
            {
                _lastWarningTime = Time.time;
                PlayClip(_lowWarningClip);
            }
        }

        // --- Battery events ---------------------------------------------------------

        private void OnToggleInput() => Toggle();

        private void OnBatteryCollected(BatteryCollectedEvent evt)
        {
            _battery.Recharge(evt.Charge);
            PlayClip(_rechargeClip);
        }

        private void OnBatteryDepleted()
        {
            // Force off with the failure sound when the battery dies mid-use.
            if (IsOn)
            {
                IsOn = false;
                if (_light != null)
                {
                    _light.enabled = false;
                }

                PlayClip(_depletedClip);
                StateChanged?.Invoke(false);
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        // --- ILightSource (AI hook) -------------------------------------------------

        /// <inheritdoc />
        public bool IsEmitting => IsOn && _light != null && _light.enabled;

        /// <inheritdoc />
        public Vector3 Position => _light != null ? _light.transform.position : transform.position;

        /// <inheritdoc />
        public float GetIlluminationAt(Vector3 worldPoint)
        {
            if (!IsEmitting)
            {
                return 0f;
            }

            Transform beam = _light.transform;
            Vector3 toPoint = worldPoint - beam.position;
            float distance = toPoint.magnitude;

            // Outside the beam's reach.
            if (distance > _light.range || distance <= 0.0001f)
            {
                return 0f;
            }

            // Outside the cone.
            Vector3 direction = toPoint / distance;
            float angle = Vector3.Angle(beam.forward, direction);
            float halfAngle = _light.spotAngle * 0.5f;
            if (angle > halfAngle)
            {
                return 0f;
            }

            // Occluded by geometry (line of sight).
            if (Physics.Raycast(beam.position, direction, distance, _occluderMask, QueryTriggerInteraction.Ignore))
            {
                return 0f;
            }

            // Falloff by distance, cone edge softness, and current flicker/brightness.
            float distanceFalloff = 1f - distance / _light.range;
            float coneFalloff = 1f - angle / Mathf.Max(0.001f, halfAngle);
            return Mathf.Clamp01(distanceFalloff * coneFalloff * _currentMultiplier);
        }

        // --- ISaveable --------------------------------------------------------------

        /// <inheritdoc />
        public string SaveId => _saveId;

        /// <inheritdoc />
        public FlashlightState CaptureState() => new FlashlightState(_battery.Charge, IsOn);

        /// <inheritdoc />
        public void RestoreState(FlashlightState state)
        {
            _battery.SetCharge(state.Charge);

            // Apply the saved on/off state directly (bypassing the toggle SFX).
            IsOn = state.IsOn && !_battery.IsEmpty;
            if (_light != null)
            {
                _light.enabled = IsOn;
            }

            _timeOn = 0f;
            StateChanged?.Invoke(IsOn);
        }
    }
}
