using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// The player's "hands". Holds one <see cref="Grabbable"/> at a time, carrying it smoothly at a
    /// hold anchor, and throws it on the interact button (or drops it via script). Implements
    /// <see cref="IInteractionOverride"/> so that, while carrying, pressing interact throws instead of
    /// operating whatever the crosshair is on.
    /// <para>
    /// Throwing re-enables physics and launches the prop; combined with an
    /// <c>ImpactNoiseEmitter</c> on the prop, a thrown object becomes a loud distraction — the
    /// intended stealth play.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeldItemController : MonoBehaviour, IInteractionOverride
    {
        [Header("Carry")]
        [Tooltip("Anchor the held object is carried at — usually a child of the camera, in front of it.")]
        [SerializeField] private Transform _holdAnchor;

        [Tooltip("Camera used to aim throws. If empty, a child camera is used.")]
        [SerializeField] private Camera _camera;

        /// <summary>Runtime wiring (used by the bootstrap; call before the object is activated).</summary>
        public void Configure(Transform holdAnchor, Camera camera)
        {
            _holdAnchor = holdAnchor;
            _camera = camera;
        }

        [Tooltip("How snappily the held object follows the anchor (higher = stiffer).")]
        [Range(2f, 40f)] [SerializeField] private float _followSharpness = 18f;

        [Tooltip("Objects heavier than this (kg) are too heavy to pick up.")]
        [Min(0.1f)] [SerializeField] private float _maxHoldMass = 15f;

        [Header("Throw")]
        [Tooltip("Launch speed applied when thrown, in m/s.")]
        [Range(1f, 20f)] [SerializeField] private float _throwSpeed = 8f;

        [Tooltip("Upward bias added to the throw direction for a natural arc.")]
        [Range(0f, 0.6f)] [SerializeField] private float _throwUpBias = 0.15f;

        private Grabbable _current;

        // Saved so the prop returns exactly to its physics settings after being carried.
        private bool _savedIsKinematic;
        private bool _savedUseGravity;
        private RigidbodyInterpolation _savedInterpolation;

        /// <summary>Whether an object is currently held.</summary>
        public bool IsHolding => _current != null;

        // --- IInteractionOverride ---------------------------------------------------

        /// <inheritdoc />
        public bool IsBlocking => IsHolding;

        /// <inheritdoc />
        public string BlockingPrompt => IsHolding ? "Throw" : string.Empty;

        /// <inheritdoc />
        public bool HandleInteract(in InteractionContext context)
        {
            if (!IsHolding)
            {
                return false;
            }

            Throw();
            return true;
        }

        // ---------------------------------------------------------------------------

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }
        }

        /// <summary>Attempts to pick up a grabbable. Fails if already holding or the object is too heavy.</summary>
        public bool TryHold(Grabbable grabbable)
        {
            if (IsHolding || grabbable == null || _holdAnchor == null)
            {
                return false;
            }

            Rigidbody body = grabbable.Body;
            if (body == null || body.mass > _maxHoldMass)
            {
                return false;
            }

            _current = grabbable;

            // Suspend physics while carried; remember settings to restore on release.
            _savedIsKinematic = body.isKinematic;
            _savedUseGravity = body.useGravity;
            _savedInterpolation = body.interpolation;

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return true;
        }

        /// <summary>Launches the held object along the camera's aim, then releases it.</summary>
        public void Throw()
        {
            if (!IsHolding)
            {
                return;
            }

            Rigidbody body = _current.Body;
            RestorePhysics(body);

            Vector3 aim = _camera != null ? _camera.transform.forward : transform.forward;
            Vector3 direction = (aim + Vector3.up * _throwUpBias).normalized;
            body.velocity = direction * _throwSpeed;

            _current = null;
        }

        /// <summary>Gently releases the held object where it is (no launch).</summary>
        public void Drop()
        {
            if (!IsHolding)
            {
                return;
            }

            RestorePhysics(_current.Body);
            _current = null;
        }

        private void RestorePhysics(Rigidbody body)
        {
            if (body == null)
            {
                return;
            }

            body.isKinematic = _savedIsKinematic;
            body.useGravity = _savedUseGravity;
            body.interpolation = _savedInterpolation;
        }

        private void FixedUpdate()
        {
            if (!IsHolding || _holdAnchor == null)
            {
                return;
            }

            Rigidbody body = _current.Body;

            // Frame-rate-independent smoothing toward the anchor, driven through the physics system
            // (MovePosition/MoveRotation) so collisions while carrying still register cleanly.
            float t = 1f - Mathf.Exp(-_followSharpness * Time.fixedDeltaTime);
            body.MovePosition(Vector3.Lerp(body.position, _holdAnchor.position, t));
            body.MoveRotation(Quaternion.Slerp(body.rotation, _holdAnchor.rotation, t));
        }
    }
}
