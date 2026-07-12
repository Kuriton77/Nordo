using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Player
{
    /// <summary>
    /// Models the player's breathing as a phase that speeds up and deepens with exertion (low
    /// stamina and sprinting). It plays optional inhale/exhale audio and, more importantly, raises
    /// a <see cref="BreathEvent"/> on each phase change — the hook every other system attaches to:
    /// cold-breath VFX emit on exhale, and later the noise/AI layer can treat heavy breathing as an
    /// audible tell.
    /// <para>
    /// This is a functional, self-contained system, not a placeholder: it produces real timing and
    /// intensity. What it deliberately leaves open (via events and the <see cref="BreathIntensity"/>
    /// property) is <em>presentation</em>, so audio and VFX can be swapped freely.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BreathController : MonoBehaviour
    {
        [Header("Locomotion Source")]
        [Tooltip("Component implementing ILocomotionState. If empty, a parent is searched automatically.")]
        [SerializeField] private MonoBehaviour _locomotionSource;

        [Header("Origin")]
        [Tooltip("Transform breaths originate from (the head/mouth). Defaults to this transform.")]
        [SerializeField] private Transform _head;

        [Header("Rate (breaths per second)")]
        [Tooltip("Calm breathing rate when fully rested.")]
        [Range(0.05f, 1f)] [SerializeField] private float _calmRate = 0.25f;

        [Tooltip("Heavy breathing rate when fully exerted.")]
        [Range(0.3f, 2f)] [SerializeField] private float _heavyRate = 0.9f;

        [Header("Exertion Model")]
        [Tooltip("How strongly low stamina raises exertion (0–1 of the total).")]
        [Range(0f, 1f)] [SerializeField] private float _staminaWeight = 0.8f;

        [Tooltip("Extra exertion added while actively sprinting.")]
        [Range(0f, 1f)] [SerializeField] private float _sprintBonus = 0.35f;

        [Tooltip("Floor on breath intensity so calm breathing is still faintly visible in the cold.")]
        [Range(0f, 0.5f)] [SerializeField] private float _minIntensity = 0.15f;

        [Tooltip("How quickly perceived exertion follows the raw value (per second). Smooths sudden jumps.")]
        [Range(0.5f, 10f)] [SerializeField] private float _exertionSmoothing = 3f;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _exhaleClips = new AudioClip[0];
        [SerializeField] private AudioClip[] _inhaleClips = new AudioClip[0];

        private ILocomotionState _locomotion;
        private float _phase;
        private bool _exhaledThisCycle;
        private float _exertion;

        /// <summary>Current breath intensity in [0, 1]; other systems read this to scale reactions.</summary>
        public float BreathIntensity => _exertion;

        private void Awake()
        {
            _locomotion = _locomotionSource as ILocomotionState ?? GetComponentInParent<ILocomotionState>();
            if (_locomotion == null)
            {
                Debug.LogError($"[BreathController] No ILocomotionState found for '{name}'.", this);
                enabled = false;
                return;
            }

            if (_head == null)
            {
                _head = transform;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Raw exertion from stamina depletion plus a sprint surcharge.
            bool sprinting = _locomotion.Stance == LocomotionStance.Sprinting;
            float rawExertion = Mathf.Clamp01((1f - _locomotion.StaminaNormalized) * _staminaWeight + (sprinting ? _sprintBonus : 0f));
            rawExertion = Mathf.Max(_minIntensity, rawExertion);

            // Smooth so intensity doesn't snap when stamina crosses thresholds.
            _exertion = Mathf.MoveTowards(_exertion, rawExertion, _exertionSmoothing * dt);

            // Advance the breath phase at a rate scaled by exertion.
            float rate = Mathf.Lerp(_calmRate, _heavyRate, _exertion);
            _phase += dt * rate;

            if (_phase >= 1f)
            {
                _phase -= 1f;
                _exhaledThisCycle = false;
                RaiseBreath(isExhale: false, _inhaleClips, volumeScale: 0.5f);
            }

            if (!_exhaledThisCycle && _phase >= 0.5f)
            {
                _exhaledThisCycle = true;
                RaiseBreath(isExhale: true, _exhaleClips, volumeScale: 1f);
            }
        }

        private void RaiseBreath(bool isExhale, AudioClip[] clips, float volumeScale)
        {
            EventBus<BreathEvent>.Raise(new BreathEvent(_exertion, isExhale, _head.position));

            if (_audioSource != null && clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null)
                {
                    float volume = Mathf.Clamp01((0.2f + 0.8f * _exertion) * volumeScale);
                    _audioSource.PlayOneShot(clip, volume);
                }
            }
        }
    }
}
