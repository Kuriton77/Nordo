using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// The reusable base for most interactables. It wires up highlighting on focus and lock-aware
    /// prompting/blocking, leaving subclasses to implement only <see cref="OnInteract"/>. Concrete
    /// interactables (Door, Drawer, ItemPickup, Grabbable, Inspectable) all build on this, which is
    /// what keeps each of them tiny and focused on its own behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("Default verb shown when focused (subclasses may override GetPrompt).")]
        [SerializeField] protected string _promptVerb = "Use";

        [Tooltip("Highlight this object while the player is looking at it.")]
        [SerializeField] private bool _highlightOnFocus = true;

        [Tooltip("Highlighter to drive. If empty, one on this object is used (optional).")]
        [SerializeField] private Highlighter _highlighter;

        private Lockable _lockable;

        /// <summary>The lock composed on this object, if any.</summary>
        protected Lockable Lock => _lockable;

        /// <summary>Convenience: true when a lock is present and engaged.</summary>
        protected bool IsLocked => _lockable != null && _lockable.IsLocked;

        /// <inheritdoc />
        public Transform Transform => transform;

        protected virtual void Awake()
        {
            if (_highlighter == null)
            {
                _highlighter = GetComponent<Highlighter>();
            }

            _lockable = GetComponent<Lockable>();
        }

        /// <inheritdoc />
        public virtual bool CanInteract(in InteractionContext context) => isActiveAndEnabled;

        /// <inheritdoc />
        public virtual string GetPrompt(in InteractionContext context)
        {
            return IsLocked ? _lockable.LockedPrompt : _promptVerb;
        }

        /// <inheritdoc />
        public virtual void OnFocusEnter(in InteractionContext context)
        {
            if (_highlightOnFocus && _highlighter != null)
            {
                _highlighter.SetHighlighted(true);
            }
        }

        /// <inheritdoc />
        public virtual void OnFocusExit(in InteractionContext context)
        {
            if (_highlighter != null)
            {
                _highlighter.SetHighlighted(false);
            }
        }

        /// <inheritdoc />
        public void Interact(in InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            // A locked object consumes the interaction as a "rattle" and does nothing else.
            if (IsLocked)
            {
                _lockable.NotifyLocked();
                return;
            }

            OnInteract(context);
        }

        /// <summary>The actual interaction behaviour, implemented by each concrete interactable.</summary>
        protected abstract void OnInteract(in InteractionContext context);
    }
}
