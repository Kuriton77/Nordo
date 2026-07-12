using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Input;

namespace Nordo.Interaction
{
    /// <summary>
    /// Runs the object-inspection mode. When asked to inspect an <see cref="Inspectable"/>, it lifts
    /// the object in front of the camera, disables its physics/colliders, locks player control (via
    /// <see cref="ControlLockEvent"/>), and lets the player spin it with the look input. Pressing
    /// interact again releases it and restores everything exactly as it was.
    /// <para>
    /// Implements <see cref="IInteractionOverride"/> so the interactor knows inspection is active and
    /// routes the next interact press here to end it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InspectionController : MonoBehaviour, IInteractionOverride
    {
        [Header("References")]
        [Tooltip("Camera the object is presented to. If empty, a child camera is used.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Shared input asset; its look axis rotates the inspected object.")]
        [SerializeField] private InputReader _input;

        [Header("Feel")]
        [Tooltip("Degrees of object rotation per unit of look input.")]
        [Range(0.02f, 1f)] [SerializeField] private float _rotateSpeed = 0.25f;

        private Inspectable _current;
        private Transform _target;

        // Saved world/transform + physics state, restored on release.
        private Transform _savedParent;
        private Vector3 _savedLocalPosition;
        private Quaternion _savedLocalRotation;
        private Vector3 _savedLocalScale;
        private Collider[] _colliders;
        private bool[] _colliderEnabled;
        private Rigidbody _rigidbody;
        private bool _savedKinematic;

        /// <summary>Whether an object is currently being inspected.</summary>
        public bool IsInspecting => _current != null;

        // --- IInteractionOverride ---------------------------------------------------

        /// <inheritdoc />
        public bool IsBlocking => IsInspecting;

        /// <inheritdoc />
        public string BlockingPrompt =>
            IsInspecting && !string.IsNullOrEmpty(_current.Caption) ? _current.Caption : "Release";

        /// <inheritdoc />
        public bool HandleInteract(in InteractionContext context)
        {
            if (!IsInspecting)
            {
                return false;
            }

            EndInspect();
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

        /// <summary>Begins inspecting the given object, taking over control until released.</summary>
        public void BeginInspect(Inspectable inspectable)
        {
            if (IsInspecting || inspectable == null || _camera == null)
            {
                return;
            }

            _current = inspectable;
            _target = inspectable.transform;

            // Save transform state.
            _savedParent = _target.parent;
            _savedLocalPosition = _target.localPosition;
            _savedLocalRotation = _target.localRotation;
            _savedLocalScale = _target.localScale;

            // Disable colliders so the object can't shove the player or block the ray.
            _colliders = _target.GetComponentsInChildren<Collider>();
            _colliderEnabled = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliderEnabled[i] = _colliders[i].enabled;
                _colliders[i].enabled = false;
            }

            // Freeze physics.
            _rigidbody = _target.GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _savedKinematic = _rigidbody.isKinematic;
                _rigidbody.isKinematic = true;
            }

            // Present the object in front of the camera.
            Transform camTransform = _camera.transform;
            _target.SetParent(camTransform, worldPositionStays: false);
            _target.localPosition = Vector3.forward * _current.InspectDistance;
            _target.localRotation = Quaternion.identity;
            _target.localScale = _savedLocalScale * _current.InspectScale;

            // Lock player movement/look for the duration.
            EventBus<ControlLockEvent>.Raise(new ControlLockEvent(true));
        }

        /// <summary>Ends inspection and restores the object and player control.</summary>
        public void EndInspect()
        {
            if (!IsInspecting)
            {
                return;
            }

            // Restore transform.
            _target.SetParent(_savedParent, worldPositionStays: false);
            _target.localPosition = _savedLocalPosition;
            _target.localRotation = _savedLocalRotation;
            _target.localScale = _savedLocalScale;

            // Restore colliders.
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                    {
                        _colliders[i].enabled = _colliderEnabled[i];
                    }
                }
            }

            // Restore physics.
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _savedKinematic;
            }

            EventBus<ControlLockEvent>.Raise(new ControlLockEvent(false));

            _current = null;
            _target = null;
            _colliders = null;
            _colliderEnabled = null;
            _rigidbody = null;
        }

        private void Update()
        {
            if (!IsInspecting || _input == null)
            {
                return;
            }

            // Spin the object with the look input, around camera-relative axes for intuitive control.
            Vector2 look = _input.LookInput;
            if (look.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Transform cam = _camera.transform;
            _target.Rotate(cam.up, -look.x * _rotateSpeed, Space.World);
            _target.Rotate(cam.right, look.y * _rotateSpeed, Space.World);
        }
    }
}
