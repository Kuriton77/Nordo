using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Interaction
{
    /// <summary>
    /// Shared behaviour for anything that opens and closes: doors, drawers, cabinets. It manages the
    /// open/closed state, a smooth eased animation, open/close audio, and a noise emission on each
    /// operation — everything except <em>how</em> the object physically moves, which subclasses
    /// provide via <see cref="ApplyOpenAmount"/>. This is the reuse backbone for all openables.
    /// <para>
    /// Procedural animation (an <see cref="AnimationCurve"/> over a normalized 0→1 progress) is used
    /// rather than baked clips, so openables work with any prop and any hinge/slide with zero art.
    /// Public <see cref="Open"/>/<see cref="Close"/>/<see cref="Toggle"/> let puzzles drive them too.
    /// </para>
    /// </summary>
    public abstract class OpenableBase : InteractableBase
    {
        [Header("Openable")]
        [SerializeField] private string _openVerb = "Open";
        [SerializeField] private string _closeVerb = "Close";

        [Tooltip("Whether the object starts open.")]
        [SerializeField] private bool _startOpen;

        [Tooltip("Seconds for a full open or close.")]
        [Range(0.05f, 3f)] [SerializeField] private float _duration = 0.6f;

        [Tooltip("Eases the 0→1 progress for a natural motion.")]
        [SerializeField] private AnimationCurve _ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private AudioClip _closeClip;

        [Header("Noise")]
        [Range(0f, 1f)] [SerializeField] private float _noiseLoudness = 0.35f;
        [Range(1f, 30f)] [SerializeField] private float _noiseRange = 10f;
        [SerializeField] private SoundPriority _noisePriority = SoundPriority.Notable;

        private float _progress;     // 0 = closed, 1 = open (raw, pre-ease)
        private float _target;
        private bool _isOpen;

        /// <summary>Whether the object is currently open (target state).</summary>
        public bool IsOpen => _isOpen;

        protected override void Awake()
        {
            base.Awake();
            CacheClosedState();

            _isOpen = _startOpen;
            _progress = _target = _isOpen ? 1f : 0f;
            ApplyOpenAmount(_ease.Evaluate(_progress));
        }

        private void Update()
        {
            if (Mathf.Approximately(_progress, _target))
            {
                return;
            }

            float step = Time.deltaTime / Mathf.Max(0.01f, _duration);
            _progress = Mathf.MoveTowards(_progress, _target, step);
            ApplyOpenAmount(_ease.Evaluate(_progress));
        }

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context)
        {
            if (IsLocked)
            {
                return base.GetPrompt(context);
            }

            return _isOpen ? _closeVerb : _openVerb;
        }

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context) => Toggle();

        /// <summary>Toggles between open and closed.</summary>
        public void Toggle() => SetOpen(!_isOpen);

        /// <summary>Opens the object (no-op if already open).</summary>
        public void Open() => SetOpen(true);

        /// <summary>Closes the object (no-op if already closed).</summary>
        public void Close() => SetOpen(false);

        /// <summary>Sets the open state, playing audio and emitting noise on a change.</summary>
        public void SetOpen(bool open)
        {
            if (open == _isOpen)
            {
                return;
            }

            _isOpen = open;
            _target = open ? 1f : 0f;

            PlayClip(open ? _openClip : _closeClip);
            EmitNoise();
        }

        private void EmitNoise()
        {
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(transform.position, _noiseLoudness, _noiseRange, _noisePriority, NoiseSourceKind.Door)));
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>Records the resting (closed) transform so motion can be applied relative to it.</summary>
        protected abstract void CacheClosedState();

        /// <summary>Applies the eased open amount in [0, 1] to the physical transform(s).</summary>
        protected abstract void ApplyOpenAmount(float amount);
    }
}
