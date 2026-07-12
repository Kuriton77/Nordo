using System.Diagnostics;
using UnityEngine;
using Nordo.Core;
using Debug = UnityEngine.Debug;

namespace Nordo.Noise
{
    /// <summary>
    /// A development tool that stress-tests the noise system for performance and reliability: it
    /// registers a configurable number of dummy listeners, fires a large burst of noises through the
    /// live <see cref="INoiseService"/>, and reports how long dispatch took and how many perceptions
    /// were delivered. Run it from the component's context menu in Play mode.
    /// <para>
    /// It validates two things: (1) dispatch stays cheap at scale (the distance early-out and
    /// non-alloc occlusion doing their job), and (2) delivery is consistent — listeners in range
    /// reliably receive audible noises.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NoiseStressTester : MonoBehaviour
    {
        [Header("Scale")]
        [Tooltip("Number of dummy listeners to register during the test.")]
        [Range(1, 500)] [SerializeField] private int _listenerCount = 50;

        [Tooltip("Number of noises to fire during the test.")]
        [Range(100, 200000)] [SerializeField] private int _noiseCount = 20000;

        [Header("World")]
        [Tooltip("Half-extent of the cube the listeners and noises are scattered within, in metres.")]
        [Range(5f, 200f)] [SerializeField] private float _areaExtent = 40f;

        [Tooltip("Range assigned to each test noise.")]
        [Range(1f, 60f)] [SerializeField] private float _noiseRange = 20f;

        /// <summary>A minimal listener that just counts what it hears (no scene object needed).</summary>
        private sealed class CountingListener : INoiseListener
        {
            private readonly Vector3 _position;
            public int HeardCount;

            public CountingListener(Vector3 position, float hearingRange)
            {
                _position = position;
                HearingRange = hearingRange;
            }

            public Vector3 Position => _position;
            public float HearingRange { get; }

            public void OnHeardNoise(in NoiseStimulus stimulus, float perceivedLoudness) => HeardCount++;
        }

        [ContextMenu("Run Stress Test")]
        public void RunStressTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[NoiseStressTester] Run this in Play mode so the NoiseSystem is live.");
                return;
            }

            if (!ServiceLocator.TryGet(out INoiseService service) || service == null)
            {
                Debug.LogError("[NoiseStressTester] No INoiseService registered. Add a NoiseSystem to the scene.");
                return;
            }

            // Register dummy listeners scattered through the area.
            var listeners = new CountingListener[_listenerCount];
            for (int i = 0; i < _listenerCount; i++)
            {
                listeners[i] = new CountingListener(RandomPoint(), 18f);
                service.RegisterListener(listeners[i]);
            }

            // Fire the burst and time it.
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < _noiseCount; i++)
            {
                var stimulus = new NoiseStimulus(RandomPoint(), 1f, _noiseRange, SoundPriority.Notable, NoiseSourceKind.Generic);
                service.ReportNoise(in stimulus);
            }
            stopwatch.Stop();

            // Tally deliveries for the reliability check.
            long totalHeard = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                totalHeard += listeners[i].HeardCount;
                service.UnregisterListener(listeners[i]);
            }

            double ms = stopwatch.Elapsed.TotalMilliseconds;
            double perNoiseUs = ms * 1000.0 / _noiseCount;
            Debug.Log($"[NoiseStressTester] {_noiseCount:N0} noises × {_listenerCount} listeners in " +
                      $"{ms:0.0} ms ({perNoiseUs:0.00} µs/noise). Deliveries: {totalHeard:N0}.");
        }

        private Vector3 RandomPoint()
        {
            return transform.position + new Vector3(
                Random.Range(-_areaExtent, _areaExtent),
                Random.Range(-_areaExtent * 0.25f, _areaExtent * 0.25f),
                Random.Range(-_areaExtent, _areaExtent));
        }
    }
}
