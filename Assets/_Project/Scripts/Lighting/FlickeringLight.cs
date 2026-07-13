using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// A dying lamp: continuous Perlin shimmer plus occasional full dropouts, like a tube on its last
    /// winter. This is a mood prop, not a system — attach beside a <see cref="Light"/> and it makes
    /// the room feel neglected. (The flashlight has its own modulator stack; this is for the world.)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlickeringLight : MonoBehaviour
    {
        [Tooltip("Light to drive. If empty, a Light on this object is used.")]
        [SerializeField] private Light _light;

        [Tooltip("Steady intensity the flicker modulates around. 0 = read from the light on Awake.")]
        [Min(0f)] [SerializeField] private float _baseIntensity;

        [Tooltip("How deep the continuous shimmer bites, in [0,1].")]
        [Range(0f, 1f)] [SerializeField] private float _flickerDepth = 0.35f;

        [Tooltip("Shimmer speed.")]
        [Range(0.5f, 30f)] [SerializeField] private float _speed = 9f;

        [Tooltip("Average full-dropout events per second (0 = never goes dark).")]
        [Range(0f, 2f)] [SerializeField] private float _dropoutsPerSecond = 0.12f;

        [Tooltip("Min/max seconds a dropout lasts.")]
        [SerializeField] private Vector2 _dropoutDuration = new Vector2(0.05f, 0.35f);

        private float _seed;
        private float _darkUntil;

        /// <summary>Runtime wiring for the level builder.</summary>
        public void Configure(Light light, float flickerDepth, float speed, float dropoutsPerSecond)
        {
            _light = light;
            _flickerDepth = flickerDepth;
            _speed = speed;
            _dropoutsPerSecond = dropoutsPerSecond;
            _baseIntensity = light != null ? light.intensity : 0f;
        }

        private void Awake()
        {
            if (_light == null)
            {
                _light = GetComponent<Light>();
            }

            _seed = Random.value * 100f;
            if (_light != null && _baseIntensity <= 0f)
            {
                _baseIntensity = _light.intensity;
            }
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            if (Time.time < _darkUntil)
            {
                _light.intensity = 0f;
                return;
            }

            if (_dropoutsPerSecond > 0f && Random.value < _dropoutsPerSecond * Time.deltaTime)
            {
                _darkUntil = Time.time + Random.Range(_dropoutDuration.x, _dropoutDuration.y);
            }

            float n = Mathf.PerlinNoise(_seed, Time.time * _speed);
            _light.intensity = _baseIntensity * (1f - _flickerDepth * n);
        }
    }
}
