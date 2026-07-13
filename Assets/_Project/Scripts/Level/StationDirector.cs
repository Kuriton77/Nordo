using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Enemy;

namespace Nordo.Level
{
    /// <summary>
    /// The section's game-flow glue: respawn the player at the entry when The Listener catches them
    /// (and send the hunter home so the encounter resets), and present the ending when the section is
    /// completed. Feedback is routed through <see cref="GameMessageEvent"/> so the <c>StationHUD</c>
    /// renders it — the director owns flow, not UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationDirector : MonoBehaviour
    {
        private Transform _player;
        private CharacterController _playerController;
        private Vector3 _spawn;
        private ListenerController _enemy;

        /// <summary>Wires the director to the built section.</summary>
        public void Configure(Transform player, Vector3 spawn, ListenerController enemy)
        {
            _player = player;
            _playerController = player != null ? player.GetComponent<CharacterController>() : null;
            _spawn = spawn;
            _enemy = enemy;
        }

        private bool _firstChaseSeen;

        private void OnEnable()
        {
            EventBus<PlayerCaughtEvent>.Subscribe(OnCaught);
            EventBus<ListenerStateChangedEvent>.Subscribe(OnListenerState);
        }

        private void OnDisable()
        {
            EventBus<PlayerCaughtEvent>.Unsubscribe(OnCaught);
            EventBus<ListenerStateChangedEvent>.Unsubscribe(OnListenerState);
        }

        /// <summary>Called when the player sends the signal at the radio console.</summary>
        public void OnSectionComplete()
        {
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent(
                "Your signal claws through the static, and far south of the ice a receiver wakes. " +
                "Behind you, Vardø-9 holds its breath — listening for what answers.  SIGNAL SENT — SECTION COMPLETE.", 10f));

            if (_enemy != null)
            {
                _enemy.enabled = false;
            }
        }

        /// <summary>
        /// The world reacts to the hunt: when The Listener breaks into a chase, the station's
        /// electrics gutter — the flashlight strobes via the emergency-flicker modulator. No scripted
        /// jump scare, just the light failing exactly when you need it most.
        /// </summary>
        private void OnListenerState(ListenerStateChangedEvent evt)
        {
            if (evt.State != ListenerStateId.Chase)
            {
                return;
            }

            EventBus<EmergencyFlickerRequestEvent>.Raise(new EmergencyFlickerRequestEvent(2.2f, 0.85f));

            if (!_firstChaseSeen)
            {
                _firstChaseSeen = true;
                EventBus<GameMessageEvent>.Raise(new GameMessageEvent(
                    "IT HEARS YOU. Break its line — then be still.", 4f));
            }
        }

        private void OnCaught(PlayerCaughtEvent evt)
        {
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent(
                "A sound. A mistake. The cold takes the rest.  —  Move slow. It cannot hear a held breath.", 4.5f));
            Respawn();
        }

        private void Respawn()
        {
            if (_player == null)
            {
                return;
            }

            if (_playerController != null)
            {
                _playerController.enabled = false;
                _player.position = _spawn;
                _playerController.enabled = true;
            }
            else
            {
                _player.position = _spawn;
            }

            if (_enemy != null && _enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            {
                _enemy.Agent.Warp(_enemy.Blackboard.HomePosition);
            }
        }
    }
}
