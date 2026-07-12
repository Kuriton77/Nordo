using UnityEngine;
using Nordo.Core;

namespace Nordo.Enemy
{
    /// <summary>
    /// The Listener's short-term memory — chiefly the last place it heard something. The whole hunt
    /// pivots on this: the enemy is blind, so "where the sound was" is the only thing it can pursue.
    /// A plain class (not a MonoBehaviour) so states share one instance cheaply.
    /// </summary>
    public sealed class ListenerBlackboard
    {
        /// <summary>World position of the most recently heard sound.</summary>
        public Vector3 LastHeardPosition { get; private set; }

        /// <summary>Time (seconds) the last sound was heard. Negative means "never".</summary>
        public float LastHeardTime { get; private set; } = -999f;

        /// <summary>Perceived loudness of the last sound, in [0, 1].</summary>
        public float LastPerceivedLoudness { get; private set; }

        /// <summary>Priority of the last sound.</summary>
        public SoundPriority LastPriority { get; private set; }

        /// <summary>The patrol/home anchor the enemy returns to.</summary>
        public Vector3 HomePosition { get; set; }

        /// <summary>Whether any sound has ever been heard.</summary>
        public bool HasMemory => LastHeardTime > 0f;

        /// <summary>Seconds since the last sound was heard.</summary>
        public float TimeSinceLastNoise => Time.time - LastHeardTime;

        /// <summary>Records a freshly heard sound as the new focus of attention.</summary>
        public void RecordNoise(Vector3 position, float perceivedLoudness, SoundPriority priority)
        {
            LastHeardPosition = position;
            LastPerceivedLoudness = perceivedLoudness;
            LastPriority = priority;
            LastHeardTime = Time.time;
        }
    }
}
