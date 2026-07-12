using UnityEngine;

namespace Nordo.Progression
{
    /// <summary>
    /// A light that only shines when its grid is powered. Off until power is restored, then it flickers
    /// on — the readable, atmospheric payoff of the generator puzzle (and, per the art direction, still
    /// a <em>limited</em> light in a mostly-dark room).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoweredLight : PoweredDevice
    {
        [Tooltip("Light(s) toggled with power. If empty, a Light on this object/children is used.")]
        [SerializeField] private Light[] _lights;

        [Tooltip("Optional emissive renderers (bulbs/panels) toggled with power.")]
        [SerializeField] private Renderer[] _emissiveRenderers;

        private void Awake()
        {
            if (_lights == null || _lights.Length == 0)
            {
                _lights = GetComponentsInChildren<Light>(true);
            }
        }

        protected override void OnPowerChanged(bool powered)
        {
            if (_lights != null)
            {
                for (int i = 0; i < _lights.Length; i++)
                {
                    if (_lights[i] != null)
                    {
                        _lights[i].enabled = powered;
                    }
                }
            }

            if (_emissiveRenderers != null)
            {
                for (int i = 0; i < _emissiveRenderers.Length; i++)
                {
                    if (_emissiveRenderers[i] != null)
                    {
                        _emissiveRenderers[i].enabled = powered;
                    }
                }
            }
        }

        /// <summary>Assigns the driven lights at runtime (used by the level builder).</summary>
        public void SetLights(params Light[] lights) => _lights = lights;
    }
}
