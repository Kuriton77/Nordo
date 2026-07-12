using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;

namespace Nordo.Lighting
{
    /// <summary>
    /// Aggregates every <see cref="ILightSource"/> so gameplay can ask "how lit is this point?" in one
    /// call. Registered in the <see cref="ServiceLocator"/> as <see cref="ILightVisibilityService"/>.
    /// <para>
    /// This is the Milestone-4 <b>AI hook</b>: the flashlight registers here and the query is fully
    /// functional, but nothing consumes it yet. The Milestone-7 enemy will call
    /// <see cref="GetIlluminationAt"/>/<see cref="IsPointLit"/> to know whether the player's beam is
    /// falling on it. Allocation-free per query.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-50)] // register before consumers query it
    [DisallowMultipleComponent]
    public sealed class LightVisibilitySystem : MonoBehaviour, ILightVisibilityService
    {
        private readonly List<ILightSource> _sources = new();

        private void Awake()
        {
            ServiceLocator.Register<ILightVisibilityService>(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out ILightVisibilityService current) && ReferenceEquals(current, this))
            {
                ServiceLocator.Unregister<ILightVisibilityService>();
            }
        }

        /// <inheritdoc />
        public void RegisterSource(ILightSource source)
        {
            if (source != null && !_sources.Contains(source))
            {
                _sources.Add(source);
            }
        }

        /// <inheritdoc />
        public void UnregisterSource(ILightSource source)
        {
            _sources.Remove(source);
        }

        /// <inheritdoc />
        public float GetIlluminationAt(Vector3 worldPoint)
        {
            float max = 0f;
            for (int i = 0; i < _sources.Count; i++)
            {
                ILightSource source = _sources[i];
                if (source == null || !source.IsEmitting)
                {
                    continue;
                }

                float value = source.GetIlluminationAt(worldPoint);
                if (value > max)
                {
                    max = value;
                    if (max >= 1f)
                    {
                        break; // can't get brighter than fully lit
                    }
                }
            }

            return max;
        }

        /// <inheritdoc />
        public bool IsPointLit(Vector3 worldPoint, float threshold)
        {
            return GetIlluminationAt(worldPoint) >= threshold;
        }
    }
}
