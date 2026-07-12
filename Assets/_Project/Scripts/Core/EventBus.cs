using System;
using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// A strongly-typed, allocation-free publish/subscribe hub.
    /// <para>
    /// Systems communicate by raising and listening for small <c>struct</c> events
    /// (see <see cref="IGameEvent"/>) instead of holding direct references to one another.
    /// This keeps the dependency graph acyclic and lets us add/remove systems without
    /// rewiring callers — the backbone of Nordo's event-driven architecture.
    /// </para>
    /// <para>
    /// Usage:
    /// <code>
    /// // Listen
    /// EventBus&lt;PlayerJumpedEvent&gt;.Subscribe(OnPlayerJumped);
    /// // Raise (no heap allocation because the event is a struct)
    /// EventBus&lt;PlayerJumpedEvent&gt;.Raise(new PlayerJumpedEvent(transform.position));
    /// // Always unsubscribe (typically in OnDisable/OnDestroy)
    /// EventBus&lt;PlayerJumpedEvent&gt;.Unsubscribe(OnPlayerJumped);
    /// </code>
    /// </para>
    /// </summary>
    /// <typeparam name="T">The event payload type. Must be a <c>struct</c> implementing
    /// <see cref="IGameEvent"/> so raising an event never allocates on the managed heap.</typeparam>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        // A multicast delegate is the cheapest correct pub/sub primitive in C#.
        // We invoke a cached local copy when raising so handlers can safely
        // unsubscribe themselves during dispatch without corrupting iteration.
        private static Action<T> _handlers;

        /// <summary>Registers a handler for events of type <typeparamref name="T"/>.</summary>
        public static void Subscribe(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            _handlers += handler;
            EventBusRegistry.Register(Clear); // allow global reset between play sessions
        }

        /// <summary>Removes a previously registered handler. Safe to call if not subscribed.</summary>
        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            _handlers -= handler;
        }

        /// <summary>
        /// Dispatches <paramref name="evt"/> to all current subscribers.
        /// Exceptions in one handler are caught and logged so a single faulty
        /// listener cannot prevent the rest from receiving the event.
        /// </summary>
        public static void Raise(T evt)
        {
            Action<T> snapshot = _handlers;
            if (snapshot == null)
            {
                return;
            }

            // Invoke each handler individually to isolate failures.
            Delegate[] invocationList = snapshot.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T>)invocationList[i]).Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Drops every subscriber for this event type. Public so the EditMode tests (a separate
        /// assembly) and hard-reset flows can restore a clean bus; domain resets also call it via
        /// <see cref="EventBusRegistry"/>.
        /// </summary>
        public static void Clear()
        {
            _handlers = null;
        }
    }

    /// <summary>
    /// Tracks the <c>Clear</c> action of every <see cref="EventBus{T}"/> that has been used,
    /// so all buses can be reset in one call. This matters when Unity's
    /// "Enter Play Mode Options" disables domain reload: static state would otherwise
    /// survive between play sessions and leak stale subscribers.
    /// </summary>
    internal static class EventBusRegistry
    {
        private static readonly System.Collections.Generic.HashSet<Action> Clears = new();

        internal static void Register(Action clear)
        {
            Clears.Add(clear);
        }

        /// <summary>Resets every known bus. Invoked automatically before a play session starts.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetAll()
        {
            foreach (Action clear in Clears)
            {
                clear?.Invoke();
            }

            Clears.Clear();
        }
    }
}
