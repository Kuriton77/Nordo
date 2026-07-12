using UnityEngine;
using Nordo.Input;

namespace Nordo.Player
{
    /// <summary>
    /// Top-level coordinator for the player rig. In Milestone 1 its job is small and focused:
    /// lock/hide the cursor for mouse-look, and toggle it back on pause. It is the natural place
    /// to later attach cross-cutting player concerns (health, cold, death) as those systems arrive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Shared input asset used by the whole player rig.")]
        [SerializeField] private InputReader _input;

        [Tooltip("Lock and hide the hardware cursor while playing.")]
        [SerializeField] private bool _lockCursorOnStart = true;

        /// <summary>Runtime wiring (used by the bootstrap; call before the object is activated).</summary>
        public void SetInput(InputReader input) => _input = input;

        /// <summary>Whether the game is currently paused. Exposed for other systems (menus, HUD).</summary>
        public bool IsPaused { get; private set; }

        private void Start()
        {
            if (_lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.PausePerformed += TogglePause;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.PausePerformed -= TogglePause;
            }
        }

        /// <summary>
        /// Toggles a lightweight pause: time freezes and the cursor is released. Movement halts
        /// naturally because <see cref="Time.deltaTime"/> becomes zero, while the camera respects
        /// <see cref="IsPaused"/> directly. The input map is intentionally left enabled so the
        /// Pause action can still fire to un-pause. A dedicated pause/UI system supersedes this in
        /// Milestone 11.
        /// </summary>
        private void TogglePause()
        {
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            SetCursorLocked(!IsPaused);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
