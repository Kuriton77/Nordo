using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nordo.Input
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> facade over the New Input System.
    /// <para>
    /// Gameplay code depends on this asset — not on <c>InputAction</c>s directly — so the
    /// entire project has a single, mockable, designer-tweakable input surface. Continuous
    /// inputs (move/look) are exposed as cached properties polled each frame; discrete inputs
    /// (jump, interact, etc.) are exposed as C# events so listeners react without polling.
    /// </para>
    /// <para>
    /// Wiring: assign the <c>NordoControls</c> <see cref="InputActionAsset"/> in the inspector,
    /// then call <see cref="EnableGameplayInput"/> (typically from the player bootstrap).
    /// One shared instance is referenced by every system that needs input.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Input/Input Reader", fileName = "InputReader")]
    public sealed class InputReader : ScriptableObject
    {
        [Header("Bindings")]
        [Tooltip("The NordoControls .inputactions asset that defines the 'Player' action map.")]
        [SerializeField] private InputActionAsset _actions;

        // --- Continuous inputs (poll these each frame) -----------------------

        /// <summary>Normalized-ish movement axis from WASD / left stick. X = strafe, Y = forward.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Raw look delta from mouse / right stick for this frame.</summary>
        public Vector2 LookInput { get; private set; }

        /// <summary>True while the sprint control is held.</summary>
        public bool SprintHeld { get; private set; }

        /// <summary>True while the crouch control is held (hold-to-crouch model).</summary>
        public bool CrouchHeld { get; private set; }

        // --- Discrete inputs (subscribe to these) ----------------------------

        /// <summary>Raised on the frame the jump button is pressed.</summary>
        public event Action JumpPerformed;

        /// <summary>Raised on the frame the interact button is pressed.</summary>
        public event Action InteractPerformed;

        /// <summary>Raised on the frame the flashlight toggle button is pressed.</summary>
        public event Action FlashlightToggled;

        /// <summary>Raised on the frame the pause button is pressed.</summary>
        public event Action PausePerformed;

        // --- Cached action handles ------------------------------------------

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _flashlightAction;
        private InputAction _pauseAction;

        private bool _initialized;

        /// <summary>
        /// Assigns the backing <see cref="InputActionAsset"/> at runtime (used by the bootstrap, which
        /// builds the controls in code). Re-initialises on the next enable.
        /// </summary>
        public void SetActions(InputActionAsset actions)
        {
            _actions = actions;
            _initialized = false;
        }

        /// <summary>
        /// Resolves and hooks all action callbacks, then enables the gameplay map.
        /// Safe to call multiple times — initialization only happens once.
        /// </summary>
        public void EnableGameplayInput()
        {
            EnsureInitialized();
            _actions.FindActionMap("Player", throwIfNotFound: true).Enable();
        }

        /// <summary>Disables the gameplay map (e.g. while paused or in menus).</summary>
        public void DisableGameplayInput()
        {
            if (_actions != null)
            {
                _actions.FindActionMap("Player")?.Disable();
            }
        }

        /// <summary>
        /// Refreshes the cached continuous values. Called by the player once per frame so
        /// every consumer of <see cref="MoveInput"/>/<see cref="LookInput"/> reads a
        /// consistent snapshot within the frame.
        /// </summary>
        public void Tick()
        {
            if (!_initialized)
            {
                return;
            }

            MoveInput = _moveAction.ReadValue<Vector2>();
            LookInput = _lookAction.ReadValue<Vector2>();
            SprintHeld = _sprintAction.IsPressed();
            CrouchHeld = _crouchAction.IsPressed();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (_actions == null)
            {
                Debug.LogError($"[InputReader] No InputActionAsset assigned on '{name}'. Assign NordoControls in the inspector.");
                return;
            }

            InputActionMap player = _actions.FindActionMap("Player", throwIfNotFound: true);
            _moveAction = player.FindAction("Move", throwIfNotFound: true);
            _lookAction = player.FindAction("Look", throwIfNotFound: true);
            _sprintAction = player.FindAction("Sprint", throwIfNotFound: true);
            _crouchAction = player.FindAction("Crouch", throwIfNotFound: true);
            _jumpAction = player.FindAction("Jump", throwIfNotFound: true);
            _interactAction = player.FindAction("Interact", throwIfNotFound: true);
            _flashlightAction = player.FindAction("Flashlight", throwIfNotFound: true);
            _pauseAction = player.FindAction("Pause", throwIfNotFound: true);

            // Route discrete "performed" callbacks to our public C# events.
            _jumpAction.performed += OnJump;
            _interactAction.performed += OnInteract;
            _flashlightAction.performed += OnFlashlight;
            _pauseAction.performed += OnPause;

            _initialized = true;
        }

        private void OnJump(InputAction.CallbackContext _) => JumpPerformed?.Invoke();
        private void OnInteract(InputAction.CallbackContext _) => InteractPerformed?.Invoke();
        private void OnFlashlight(InputAction.CallbackContext _) => FlashlightToggled?.Invoke();
        private void OnPause(InputAction.CallbackContext _) => PausePerformed?.Invoke();

        /// <summary>
        /// Unhooks callbacks and disables input when the asset leaves memory. This prevents
        /// stale delegate references from surviving into the next play session when domain
        /// reload is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            _jumpAction.performed -= OnJump;
            _interactAction.performed -= OnInteract;
            _flashlightAction.performed -= OnFlashlight;
            _pauseAction.performed -= OnPause;

            DisableGameplayInput();
            _initialized = false;
        }
    }
}
