using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nordo.Audio
{
    /// <summary>
    /// Resolves "what am I standing on?" into a <see cref="SurfaceDefinition"/>. Resolution order,
    /// most specific first:
    /// <list type="number">
    /// <item>A <see cref="SurfaceIdentifier"/> on the collider (or its parents) — explicit authoring.</item>
    /// <item>A mapping from the collider's <see cref="PhysicMaterial"/> — bulk assignment via physics.</item>
    /// <item>The configured default surface — a guaranteed fallback so a footstep never goes silent.</item>
    /// </list>
    /// Kept as a <see cref="ScriptableObject"/> so the whole mapping is a shared, versioned asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Audio/Surface Library", fileName = "SurfaceLibrary")]
    public sealed class SurfaceLibrary : ScriptableObject
    {
        /// <summary>A single physics-material → surface mapping row.</summary>
        [Serializable]
        private struct MaterialMapping
        {
            [Tooltip("The physics material found on a collider.")]
            public PhysicMaterial Material;

            [Tooltip("The surface to use when that physics material is encountered.")]
            public SurfaceDefinition Surface;
        }

        [Header("Fallback")]
        [Tooltip("Used when no more specific surface can be identified. Should always be assigned.")]
        [SerializeField] private SurfaceDefinition _default;

        [Header("Physics-Material Mappings")]
        [SerializeField] private MaterialMapping[] _materialMappings = new MaterialMapping[0];

        // Built lazily from _materialMappings for O(1) lookups at runtime.
        private Dictionary<PhysicMaterial, SurfaceDefinition> _materialLookup;

        /// <summary>The guaranteed fallback surface.</summary>
        public SurfaceDefinition Default => _default;

        /// <summary>
        /// Resolves the surface for a raycast hit against the floor.
        /// Never returns null unless the default is unassigned (which is logged).
        /// </summary>
        public SurfaceDefinition Resolve(in RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null)
            {
                return _default;
            }

            // 1) Explicit tag on the collider or a parent.
            SurfaceIdentifier identifier = collider.GetComponentInParent<SurfaceIdentifier>();
            if (identifier != null && identifier.Surface != null)
            {
                return identifier.Surface;
            }

            // 2) Physics-material mapping.
            PhysicMaterial material = collider.sharedMaterial;
            if (material != null)
            {
                EnsureLookupBuilt();
                if (_materialLookup.TryGetValue(material, out SurfaceDefinition mapped) && mapped != null)
                {
                    return mapped;
                }
            }

            // 3) Fallback.
            if (_default == null)
            {
                Debug.LogWarning($"[SurfaceLibrary] '{name}' has no default surface assigned; footstep produced no sound.", this);
            }

            return _default;
        }

        private void EnsureLookupBuilt()
        {
            if (_materialLookup != null)
            {
                return;
            }

            _materialLookup = new Dictionary<PhysicMaterial, SurfaceDefinition>();
            for (int i = 0; i < _materialMappings.Length; i++)
            {
                MaterialMapping mapping = _materialMappings[i];
                if (mapping.Material != null && mapping.Surface != null)
                {
                    _materialLookup[mapping.Material] = mapping.Surface;
                }
            }
        }

        // Rebuild the lookup if the asset is edited at runtime (e.g. in the editor).
        private void OnValidate() => _materialLookup = null;
    }
}
