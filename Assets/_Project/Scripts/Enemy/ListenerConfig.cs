using UnityEngine;
using Nordo.Core;

namespace Nordo.Enemy
{
    /// <summary>
    /// All of The Listener's tunable behaviour and difficulty parameters, as a
    /// <see cref="ScriptableObject"/>. Author Easy/Normal/Hard assets and swap them to retune the
    /// hunt without code. If a controller has none assigned, it creates a default instance at runtime,
    /// so The Listener always works out of the box.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Enemy/Listener Config", fileName = "ListenerConfig_")]
    public sealed class ListenerConfig : ScriptableObject
    {
        [Header("Hearing")]
        [Tooltip("Absolute hearing radius. Sounds beyond both this and their own range are unheard.")]
        [Range(4f, 60f)] public float HearingRange = 22f;

        [Header("Movement Speeds (m/s)")]
        [Range(0.3f, 4f)] public float PatrolSpeed = 1.1f;
        [Range(0.5f, 5f)] public float InvestigateSpeed = 1.9f;
        [Range(1f, 8f)] public float ChaseSpeed = 3.4f;
        [Range(0.5f, 5f)] public float ReturnSpeed = 1.6f;

        [Tooltip("How fast it rotates to face movement/target, in degrees per second.")]
        [Range(60f, 720f)] public float AngularSpeed = 320f;

        [Header("Suspicion (0–1)")]
        [Tooltip("Suspicion gained per unit of perceived loudness heard.")]
        [Range(0.1f, 5f)] public float SuspicionPerLoudness = 1.6f;

        [Tooltip("Suspicion lost per second when nothing is heard.")]
        [Range(0.02f, 1f)] public float SuspicionDecayPerSecond = 0.18f;

        [Tooltip("Suspicion at/above which it leaves patrol to investigate.")]
        [Range(0f, 1f)] public float InvestigateThreshold = 0.35f;

        [Tooltip("Suspicion at/above which it breaks into a chase.")]
        [Range(0f, 1f)] public float ChaseThreshold = 0.75f;

        [Header("Investigate / Search")]
        [Tooltip("Seconds spent looking around after reaching a sound.")]
        [Range(0.5f, 8f)] public float InvestigateLookTime = 2.5f;

        [Tooltip("Seconds spent sweeping before giving up the search.")]
        [Range(2f, 20f)] public float SearchDuration = 8f;

        [Tooltip("Radius around the last sound that search points are sampled within.")]
        [Range(1f, 15f)] public float SearchRadius = 6f;

        [Header("Chase")]
        [Tooltip("Seconds of total silence before a chase is lost (the core 'go quiet to escape' window).")]
        [Range(0.5f, 8f)] public float LoseTargetTime = 3.5f;

        [Header("Attack")]
        [Tooltip("Distance at which it can strike the player.")]
        [Range(0.5f, 4f)] public float AttackRange = 1.7f;

        [Tooltip("Wind-up before the strike lands, in seconds.")]
        [Range(0.1f, 1.5f)] public float AttackWindup = 0.45f;

        [Tooltip("Cooldown after an attack before it can act again, in seconds.")]
        [Range(0.3f, 3f)] public float AttackCooldown = 1.2f;

        [Header("Navigation")]
        [Tooltip("Distance to a destination that counts as 'arrived'.")]
        [Range(0.2f, 3f)] public float ReachThreshold = 0.6f;

        [Tooltip("How far ahead it probes for closed doors to push open.")]
        [Range(0.5f, 3f)] public float DoorProbeDistance = 1.4f;
    }
}
