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

        private void OnEnable()
        {
            EventBus<PlayerCaughtEvent>.Subscribe(OnCaught);
        }

        private void OnDisable()
        {
            EventBus<PlayerCaughtEvent>.Unsubscribe(OnCaught);
        }

        /// <summary>Called when the player reaches the exit.</summary>
        public void OnSectionComplete()
        {
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent(
                "You restored power to Vardø-9 and slipped into the dark beyond. The relay is awake — and so is it.", 8f));

            if (_enemy != null)
            {
                _enemy.enabled = false;
            }
        }

        private void OnCaught(PlayerCaughtEvent evt)
        {
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent("It found you. Stay silent — move between its passes.", 4f));
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
