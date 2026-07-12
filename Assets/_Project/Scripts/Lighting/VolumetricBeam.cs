using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// Drives a volumetric light-shaft renderer (a translucent cone mesh under the light) so the beam
    /// is visible in the air, matching the light's colour and brightness in real time. URP has no
    /// built-in per-light volumetrics, so this provides the <em>support</em>: assign a cone renderer
    /// with a transparent/additive material and this keeps it in sync — including reacting to flicker.
    /// <para>
    /// Uses a <see cref="MaterialPropertyBlock"/> (no material instancing) and is fully null-safe, so
    /// it can be attached before the art exists.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VolumetricBeam : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The light whose colour/intensity the shaft follows. If empty, a parent light is used.")]
        [SerializeField] private Light _light;

        [Tooltip("Renderer of the cone/shaft mesh to tint. If empty, this object's renderer is used.")]
        [SerializeField] private Renderer _beamRenderer;

        [Header("Shaping")]
        [Tooltip("Light intensity that maps to full shaft opacity.")]
        [Range(0.5f, 20f)] [SerializeField] private float _intensityReference = 4.5f;

        [Tooltip("Maximum shaft opacity at full intensity.")]
        [Range(0f, 1f)] [SerializeField] private float _maxAlpha = 0.35f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            if (_light == null)
            {
                _light = GetComponentInParent<Light>();
            }

            if (_beamRenderer == null)
            {
                _beamRenderer = GetComponent<Renderer>();
            }

            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_light == null || _beamRenderer == null)
            {
                return;
            }

            // Hide the shaft entirely when the light is off.
            bool emitting = _light.enabled && _light.intensity > 0.001f;
            if (_beamRenderer.enabled != emitting)
            {
                _beamRenderer.enabled = emitting;
            }

            if (!emitting)
            {
                return;
            }

            float alpha = Mathf.Clamp01(_light.intensity / _intensityReference) * _maxAlpha;
            Color color = _light.color;
            color.a = alpha;

            _beamRenderer.GetPropertyBlock(_block);
            // Set both common colour properties so it works with URP/Lit and Unlit shaders alike.
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            _beamRenderer.SetPropertyBlock(_block);
        }
    }
}
