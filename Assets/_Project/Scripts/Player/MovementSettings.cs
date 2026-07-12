using UnityEngine;

namespace Nordo.Player
{
    /// <summary>
    /// Designer-authored tuning data for the first-person motor. Kept as a
    /// <see cref="ScriptableObject"/> so movement feel can be balanced without code changes
    /// and so different characters/difficulties can reference distinct presets.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Player/Movement Settings", fileName = "MovementSettings")]
    public sealed class MovementSettings : ScriptableObject
    {
        [Header("Speeds (metres / second)")]
        [Tooltip("Base walking speed.")]
        [Range(0.5f, 8f)] public float WalkSpeed = 2.6f;

        [Tooltip("Sprint speed. In Nordo, faster movement is louder — sprinting is a deliberate risk.")]
        [Range(1f, 12f)] public float SprintSpeed = 5.2f;

        [Tooltip("Speed while crouched — the quietest way to travel.")]
        [Range(0.3f, 5f)] public float CrouchSpeed = 1.3f;

        [Header("Acceleration")]
        [Tooltip("How quickly horizontal velocity ramps toward the target speed. Higher = snappier.")]
        [Range(1f, 30f)] public float Acceleration = 12f;

        [Tooltip("How quickly horizontal velocity decays when there is no input. Higher = less slide.")]
        [Range(1f, 30f)] public float Deceleration = 16f;

        [Header("Crouch")]
        [Tooltip("Standing capsule height.")]
        [Range(1.2f, 2.2f)] public float StandingHeight = 1.8f;

        [Tooltip("Crouched capsule height.")]
        [Range(0.5f, 1.6f)] public float CrouchHeight = 1.0f;

        [Tooltip("Seconds to blend between standing and crouched height.")]
        [Range(0.02f, 0.5f)] public float CrouchTransitionTime = 0.12f;

        [Header("Jump & Gravity")]
        [Tooltip("Peak jump height in metres. Horror movement is grounded — keep this modest.")]
        [Range(0f, 2f)] public float JumpHeight = 0.9f;

        [Tooltip("Gravity magnitude. Stronger than Earth's for a weightier, less floaty feel.")]
        [Range(9.81f, 40f)] public float Gravity = 22f;

        [Tooltip("Small downward force applied while grounded to keep the controller pinned to slopes/steps.")]
        [Range(0f, 5f)] public float GroundStickForce = 2f;

        [Header("Stamina (consumed by sprinting)")]
        [Tooltip("Maximum stamina pool, in seconds of continuous sprint.")]
        [Range(1f, 20f)] public float MaxStamina = 6f;

        [Tooltip("Stamina drained per second while sprinting.")]
        [Range(0.1f, 5f)] public float StaminaDrainPerSecond = 1f;

        [Tooltip("Stamina regenerated per second while not sprinting.")]
        [Range(0.1f, 5f)] public float StaminaRegenPerSecond = 0.75f;

        [Tooltip("Delay after sprinting before stamina begins to regenerate.")]
        [Range(0f, 3f)] public float StaminaRegenDelay = 1f;
    }
}
