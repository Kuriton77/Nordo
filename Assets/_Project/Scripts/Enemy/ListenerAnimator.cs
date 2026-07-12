using UnityEngine;
using Nordo.Core;

namespace Nordo.Enemy
{
    /// <summary>
    /// The animation contract for The Listener. The controller talks to this interface, never to a
    /// Unity <c>Animator</c> directly, so the creature can be animated, swapped, or run headless (for
    /// tests / greybox) without the AI caring.
    /// </summary>
    public interface IListenerAnimator
    {
        /// <summary>Sets the locomotion blend value (0 = idle, 1 = full run).</summary>
        void SetLocomotion(float normalizedSpeed);

        /// <summary>Notifies the animator of a behavioural state change.</summary>
        void SetState(ListenerStateId state);

        /// <summary>Fires the attack animation.</summary>
        void TriggerAttack();
    }

    /// <summary>
    /// Default <see cref="IListenerAnimator"/> that drives a Unity <see cref="Animator"/> through a
    /// small, documented parameter contract:
    /// <list type="bullet">
    /// <item><c>float  Speed</c> — locomotion blend (0–1)</item>
    /// <item><c>int    State</c> — the <see cref="ListenerStateId"/> value</item>
    /// <item><c>trigger Attack</c> — play the strike</item>
    /// </list>
    /// It is completely null-safe: with no Animator assigned it does nothing, so The Listener is fully
    /// functional as an untextured capsule during greyboxing and only "wears its skin" once an Animator
    /// Controller with these parameters is attached.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ListenerAnimator : MonoBehaviour, IListenerAnimator
    {
        [Tooltip("Animator to drive. Optional — if empty the hooks are no-ops (greybox-friendly).")]
        [SerializeField] private Animator _animator;

        [Tooltip("How quickly the Speed parameter eases toward its target, per second.")]
        [Range(1f, 20f)] [SerializeField] private float _speedDamping = 8f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int StateHash = Animator.StringToHash("State");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private float _speed;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        /// <inheritdoc />
        public void SetLocomotion(float normalizedSpeed)
        {
            _speed = Mathf.MoveTowards(_speed, Mathf.Clamp01(normalizedSpeed), _speedDamping * Time.deltaTime);
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, _speed);
            }
        }

        /// <inheritdoc />
        public void SetState(ListenerStateId state)
        {
            if (_animator != null)
            {
                _animator.SetInteger(StateHash, (int)state);
            }
        }

        /// <inheritdoc />
        public void TriggerAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }
        }
    }
}
