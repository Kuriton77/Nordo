using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Input;

namespace Nordo.Interaction
{
    /// <summary>
    /// The player's interaction driver. Each frame it casts a short ray from the camera, tracks the
    /// focused <see cref="IInteractable"/> (raising focus enter/exit for highlighting), and publishes
    /// the prompt text. On the interact button it first offers the press to any
    /// <see cref="IInteractionOverride"/> (hold/inspect), then falls back to interacting with the
    /// focused object.
    /// <para>
    /// Place this on the <b>player root</b>; assign the actual Camera. It is the only class that
    /// knows about the interaction loop — everything else just implements the interfaces.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(60)]
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera whose forward defines the look ray. If empty, a child camera is used.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Shared input asset providing the Interact action.")]
        [SerializeField] private InputReader _input;

        /// <summary>Runtime wiring (used by the bootstrap; call before the object is activated).</summary>
        public void Configure(InputReader input, Camera camera)
        {
            _input = input;
            _camera = camera;
        }

        [Header("Ray")]
        [Tooltip("Maximum interaction distance in metres.")]
        [Range(0.5f, 6f)] [SerializeField] private float _range = 2.6f;

        [Tooltip("Cast radius — a thin sphere makes small objects easier to target.")]
        [Range(0f, 0.2f)] [SerializeField] private float _castRadius = 0.05f;

        [Tooltip("Layers the interaction ray can hit.")]
        [SerializeField] private LayerMask _mask = ~0;

        private IInteractionOverride[] _overrides;
        private IInteractable _focused;
        private bool _promptVisible;
        private string _promptText = string.Empty;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }

            // Cache every override on the player (held-object, inspection…), including inactive ones.
            _overrides = GetComponentsInChildren<IInteractionOverride>(true);
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.InteractPerformed += OnInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.InteractPerformed -= OnInteractPressed;
            }

            ClearFocus();
            SetPrompt(false, string.Empty);
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            // 1) A blocking override (inspecting/holding) owns the screen — suppress world focus.
            IInteractionOverride blocker = GetBlockingOverride();
            if (blocker != null)
            {
                ClearFocus();
                SetPrompt(!string.IsNullOrEmpty(blocker.BlockingPrompt), blocker.BlockingPrompt);
                return;
            }

            // 2) Normal focus: raycast for an interactable.
            if (TryFindInteractable(out IInteractable interactable, out InteractionContext context)
                && interactable.CanInteract(context))
            {
                SetFocus(interactable, context);
                SetPrompt(true, interactable.GetPrompt(context));
            }
            else
            {
                ClearFocus();
                SetPrompt(false, string.Empty);
            }
        }

        private void OnInteractPressed()
        {
            InteractionContext context = TryFindInteractable(out IInteractable interactable, out InteractionContext ctx)
                ? ctx
                : BuildContext(_camera.transform.position + _camera.transform.forward * _range, -_camera.transform.forward, _range);

            // Overrides get first refusal (e.g. throw the held object instead of opening a door).
            for (int i = 0; i < _overrides.Length; i++)
            {
                IInteractionOverride ovr = _overrides[i];
                if (ovr != null && ovr.HandleInteract(context))
                {
                    return;
                }
            }

            if (interactable != null && interactable.CanInteract(context))
            {
                interactable.Interact(context);
            }
        }

        /// <summary>Casts the look ray and resolves the interactable it hits (if any).</summary>
        private bool TryFindInteractable(out IInteractable interactable, out InteractionContext context)
        {
            Transform cam = _camera.transform;
            Vector3 origin = cam.position;
            Vector3 direction = cam.forward;

            RaycastHit hit;
            bool hitSomething = _castRadius > 0f
                ? Physics.SphereCast(origin, _castRadius, direction, out hit, _range, _mask, QueryTriggerInteraction.Collide)
                : Physics.Raycast(origin, direction, out hit, _range, _mask, QueryTriggerInteraction.Collide);

            if (hitSomething)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    context = BuildContext(hit.point, hit.normal, hit.distance);
                    return true;
                }
            }

            interactable = null;
            context = default;
            return false;
        }

        private InteractionContext BuildContext(Vector3 point, Vector3 normal, float distance)
        {
            return new InteractionContext(gameObject, _camera, point, normal, distance);
        }

        private void SetFocus(IInteractable interactable, in InteractionContext context)
        {
            if (ReferenceEquals(interactable, _focused))
            {
                return;
            }

            ClearFocus();
            _focused = interactable;
            _focused.OnFocusEnter(context);
        }

        private void ClearFocus()
        {
            if (_focused == null)
            {
                return;
            }

            InteractionContext empty = default;
            _focused.OnFocusExit(empty);
            _focused = null;
        }

        /// <summary>Publishes the prompt only when it actually changes, to avoid event spam.</summary>
        private void SetPrompt(bool visible, string text)
        {
            if (visible == _promptVisible && text == _promptText)
            {
                return;
            }

            _promptVisible = visible;
            _promptText = text;
            EventBus<InteractionPromptEvent>.Raise(new InteractionPromptEvent(visible, text));
        }

        private IInteractionOverride GetBlockingOverride()
        {
            for (int i = 0; i < _overrides.Length; i++)
            {
                if (_overrides[i] != null && _overrides[i].IsBlocking)
                {
                    return _overrides[i];
                }
            }

            return null;
        }
    }
}
