using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// Highlights an object by driving its emission colour via a <see cref="MaterialPropertyBlock"/>.
    /// Using a property block means <b>no material instancing</b> — the shared material and SRP
    /// batching are preserved, which is essential for keeping large, prop-heavy levels performant.
    /// <para>
    /// <b>Setup:</b> the target renderers' materials must have <i>Emission enabled</i> (a URP/Lit
    /// checkbox), typically with a black emission colour at rest. This component then lifts the
    /// emission on focus and drops it back to black on defocus.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Highlighter : MonoBehaviour
    {
        [Tooltip("Renderers to highlight. If empty, all child renderers are used.")]
        [SerializeField] private Renderer[] _targets;

        [Tooltip("Emission colour applied while highlighted.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _highlightEmission = new Color(0.55f, 0.5f, 0.35f) * 1.5f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;
        private bool _isHighlighted;

        /// <summary>Whether the object is currently highlighted.</summary>
        public bool IsHighlighted => _isHighlighted;

        private void Awake()
        {
            if (_targets == null || _targets.Length == 0)
            {
                _targets = GetComponentsInChildren<Renderer>();
            }

            _block = new MaterialPropertyBlock();
        }

        /// <summary>Turns the highlight on or off. Cheap and idempotent.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (highlighted == _isHighlighted)
            {
                return;
            }

            _isHighlighted = highlighted;
            Color emission = highlighted ? _highlightEmission : Color.black;

            for (int i = 0; i < _targets.Length; i++)
            {
                Renderer renderer = _targets[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
