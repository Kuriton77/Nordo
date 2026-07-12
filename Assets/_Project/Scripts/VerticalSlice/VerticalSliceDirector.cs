using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Enemy;

namespace Nordo.VerticalSlice
{
    /// <summary>
    /// Runs the test-area gameplay loop and its debug HUD: it shows the objective and a live read-out
    /// of The Listener (state + suspicion), handles being caught (respawn at spawn, reset the hunter),
    /// and handles escaping (win). This is the thin "game flow" glue that makes the slice finishable —
    /// the real systems do all the work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerticalSliceDirector : MonoBehaviour
    {
        private Transform _player;
        private CharacterController _playerController;
        private Vector3 _spawnPoint;
        private ListenerController _enemy;

        private string _message = string.Empty;
        private float _messageUntil;
        private int _deaths;
        private bool _escaped;

        /// <summary>Wires the director to the built scene. Called by the builder.</summary>
        public void Configure(Transform player, Vector3 spawnPoint, ListenerController enemy)
        {
            _player = player;
            _playerController = player != null ? player.GetComponent<CharacterController>() : null;
            _spawnPoint = spawnPoint;
            _enemy = enemy;
        }

        private void OnEnable()
        {
            EventBus<PlayerCaughtEvent>.Subscribe(OnPlayerCaught);
        }

        private void OnDisable()
        {
            EventBus<PlayerCaughtEvent>.Unsubscribe(OnPlayerCaught);
        }

        /// <summary>Called by the ExitZone when the player escapes.</summary>
        public void OnPlayerEscaped()
        {
            if (_escaped)
            {
                return;
            }

            _escaped = true;
            ShowMessage("You slipped out into the dark. You escaped Vardø-9.", 999f);

            // Stand the hunter down.
            if (_enemy != null)
            {
                _enemy.enabled = false;
            }
        }

        private void OnPlayerCaught(PlayerCaughtEvent evt)
        {
            _deaths++;
            ShowMessage("The Listener found you. Try to stay quiet.", 3.5f);
            Respawn();
        }

        private void Respawn()
        {
            if (_player == null)
            {
                return;
            }

            // Teleport the player back to spawn (disable the controller so the move sticks).
            if (_playerController != null)
            {
                _playerController.enabled = false;
                _player.position = _spawnPoint;
                _playerController.enabled = true;
            }
            else
            {
                _player.position = _spawnPoint;
            }

            // Send the hunter home so the encounter can reset.
            if (_enemy != null && _enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            {
                _enemy.Agent.Warp(_enemy.Blackboard.HomePosition);
            }
        }

        private void ShowMessage(string message, float duration)
        {
            _message = message;
            _messageUntil = Time.time + duration;
        }

        private void OnGUI()
        {
            const int pad = 12;
            GUI.Box(new Rect(pad, pad, 360, 96), "VARDØ-9 — TEST AREA");

            GUI.Label(new Rect(pad + 8, pad + 24, 340, 20),
                "Objective: reach the EXIT. Stay quiet — throw things to distract.");

            if (_enemy != null)
            {
                GUI.Label(new Rect(pad + 8, pad + 44, 340, 20),
                    $"Listener: {_enemy.CurrentState}");

                // Suspicion meter.
                var barBg = new Rect(pad + 8, pad + 66, 200, 14);
                GUI.Box(barBg, GUIContent.none);
                var fill = new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(_enemy.Suspicion), barBg.height);
                Color prev = GUI.color;
                GUI.color = Color.Lerp(Color.green, Color.red, _enemy.Suspicion);
                GUI.Box(fill, GUIContent.none);
                GUI.color = prev;
                GUI.Label(new Rect(barBg.x + 210, barBg.y - 2, 120, 20), $"Suspicion  (deaths: {_deaths})");
            }

            if (Time.time < _messageUntil && !string.IsNullOrEmpty(_message))
            {
                var box = new Rect(Screen.width * 0.5f - 220, Screen.height - 80, 440, 40);
                GUI.Box(box, _message);
            }
        }
    }
}
