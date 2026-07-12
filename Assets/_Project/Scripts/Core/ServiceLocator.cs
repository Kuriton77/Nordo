using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// A minimal, interface-keyed registry for long-lived cross-cutting services
    /// (e.g. the noise system, save service, audio manager).
    /// <para>
    /// It exists to avoid a sprawl of <c>MonoBehaviour</c> singletons: systems register
    /// themselves at bootstrap and consumers resolve by <em>interface</em>, so concrete
    /// implementations can be swapped (real vs. test/mock) without touching call sites.
    /// This is a pragmatic Service Locator — used only for genuinely global services;
    /// ordinary object-to-object communication should still prefer the <see cref="EventBus{T}"/>.
    /// </para>
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        /// <summary>
        /// Registers <paramref name="service"/> under the interface/type <typeparamref name="T"/>.
        /// A second registration overwrites the first and logs a warning, which surfaces
        /// accidental double-registration during development.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Attempted to register a null service for '{typeof(T).Name}'.");
                return;
            }

            Type key = typeof(T);
            if (Services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceLocator] Service '{key.Name}' is already registered; overwriting.");
            }

            Services[key] = service;
        }

        /// <summary>Removes the service registered under <typeparamref name="T"/>, if any.</summary>
        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        /// <summary>
        /// Returns the service registered under <typeparamref name="T"/>,
        /// or <c>null</c> if none is registered (with a warning to aid debugging).
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            Debug.LogWarning($"[ServiceLocator] No service registered for '{typeof(T).Name}'.");
            return null;
        }

        /// <summary>Attempts to resolve a service without logging when it is absent.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Clears all registrations. Called on domain reset so no stale services leak between play sessions.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Services.Clear();
        }
    }
}
