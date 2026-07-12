using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// Something that can hear noises — the enemy's ears in Milestone 7, a debug visualizer today.
    /// Listeners register with the <see cref="INoiseService"/>; the service filters stimuli by
    /// distance/occlusion and calls <see cref="OnHeardNoise"/> only for sounds this listener can
    /// actually perceive, with the already-attenuated loudness so the AI needn't recompute it.
    /// </summary>
    public interface INoiseListener
    {
        /// <summary>Current world position of the listener (its "ears").</summary>
        Vector3 Position { get; }

        /// <summary>
        /// Absolute hearing cutoff in metres. Sounds whose effective range and this radius both fall
        /// short of the distance are never delivered — a cheap early-out that keeps large levels fast.
        /// </summary>
        float HearingRange { get; }

        /// <summary>
        /// Invoked when this listener hears a noise it can perceive.
        /// </summary>
        /// <param name="stimulus">The original stimulus (position, priority, kind…).</param>
        /// <param name="perceivedLoudness">Loudness after distance + occlusion attenuation, in [0, 1].</param>
        void OnHeardNoise(in NoiseStimulus stimulus, float perceivedLoudness);
    }
}
