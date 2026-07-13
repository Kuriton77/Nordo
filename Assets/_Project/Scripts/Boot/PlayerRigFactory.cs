using UnityEngine;
using UnityEngine.InputSystem;
using Nordo.Input;
using Nordo.Player;
using Nordo.CameraFeel;
using Nordo.Lighting;
using Nordo.Audio;
using Nordo.Noise;
using Nordo.Interaction;

namespace Nordo.Boot
{
    /// <summary>
    /// Builds a complete, wired first-person player rig entirely in code, so the startup scene is
    /// playable with no hand-authored prefab. It uses the "build inactive → wire → activate" pattern:
    /// the whole hierarchy is created inactive, every component's references are set via its runtime
    /// <c>Configure</c>/<c>Set*</c> methods, and only then is the root activated — so each
    /// <c>Awake</c>/<c>OnEnable</c> runs with its dependencies already in place.
    /// <para>
    /// Controls are built from an embedded copy of the NordoControls JSON via
    /// <see cref="InputActionAsset.FromJson"/>, avoiding any asset reference. Tuning uses runtime
    /// <see cref="ScriptableObject"/> instances (the locked defaults), which the player can later swap
    /// for authored assets.
    /// </para>
    /// </summary>
    public static class PlayerRigFactory
    {
        /// <summary>Constructs the player rig at <paramref name="spawn"/> and returns its root.</summary>
        public static GameObject Build(Vector3 spawn)
        {
            // --- Input (built from embedded JSON, no asset needed) ---
            InputActionAsset actions = InputActionAsset.FromJson(ControlsJson);
            InputReader reader = ScriptableObject.CreateInstance<InputReader>();
            reader.name = "InputReader(runtime)";
            reader.SetActions(actions);

            // --- Hierarchy, created inactive so Awake/OnEnable are deferred until fully wired ---
            var player = new GameObject("Player") { tag = "Player" };
            player.SetActive(false);
            player.transform.position = spawn;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var rigGo = new GameObject("CameraRig");
            rigGo.transform.SetParent(pivot, false);

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(rigGo.transform, false);
            Camera camera = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var holdAnchor = new GameObject("HoldAnchor").transform;
            holdAnchor.SetParent(camGo.transform, false);
            holdAnchor.localPosition = new Vector3(0f, -0.25f, 0.75f);

            var flashGo = new GameObject("Flashlight");
            flashGo.transform.SetParent(camGo.transform, false);
            Light spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.enabled = false;

            // --- Locomotion / look / control ---
            player.AddComponent<FirstPersonMotor>().Configure(reader, ScriptableObject.CreateInstance<MovementSettings>());
            player.AddComponent<PlayerLook>().Configure(reader, pivot);
            player.AddComponent<PlayerController>().SetInput(reader);

            // --- Interaction ---
            player.AddComponent<PlayerInteractor>().Configure(reader, camera);
            player.AddComponent<HeldItemController>().Configure(holdAnchor, camera);
            player.AddComponent<InspectionController>().Configure(reader, camera);

            // --- Footsteps: audible, surface-aware. Concrete scuffs by default; the level's metal
            // floors override via SurfaceIdentifier. Every step is also a stimulus for The Listener.
            var library = ScriptableObject.CreateInstance<SurfaceLibrary>();
            library.SetDefaultSurface(SurfaceDefinition.CreateRuntime(
                Nordo.Core.SurfaceKind.Concrete,
                Nordo.Audio.ProceduralAudio.FootstepSet(metal: false),
                new[] { Nordo.Audio.ProceduralAudio.LandThud() },
                noiseLoudness: 0.6f, baseVolume: 0.55f));
            player.AddComponent<FootstepController>().SetSurfaceLibrary(library);

            player.AddComponent<BreathController>();
            player.AddComponent<PlayerNoiseEmitter>();

            // --- Camera feel stack ---
            rigGo.AddComponent<HeadBobEffect>();
            rigGo.AddComponent<CameraSwayEffect>();
            rigGo.AddComponent<LandingImpactEffect>();
            rigGo.AddComponent<MoveTiltEffect>();
            rigGo.AddComponent<CameraRig>().SetAimSources(player.transform, pivot);

            // --- Flashlight ---
            flashGo.AddComponent<FlashlightController>().Configure(reader, spot, ScriptableObject.CreateInstance<FlashlightSettings>());

            // Everything wired — bring it to life.
            player.SetActive(true);
            return player;
        }

        // Embedded copy of Assets/_Project/Input/NordoControls (keyboard & mouse) so the rig needs no
        // asset reference. Loaded with InputActionAsset.FromJson.
        private const string ControlsJson = @"{
  ""name"": ""NordoControls"",
  ""maps"": [
    {
      ""name"": ""Player"",
      ""id"": ""a1b2c3d4-0000-4000-8000-000000000001"",
      ""actions"": [
        { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000101"", ""expectedControlType"": ""Vector2"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": true },
        { ""name"": ""Look"", ""type"": ""Value"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000102"", ""expectedControlType"": ""Vector2"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": true },
        { ""name"": ""Sprint"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000103"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
        { ""name"": ""Crouch"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000104"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
        { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000105"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": ""Press"", ""initialStateCheck"": false },
        { ""name"": ""Interact"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000106"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": ""Press"", ""initialStateCheck"": false },
        { ""name"": ""Flashlight"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000107"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": ""Press"", ""initialStateCheck"": false },
        { ""name"": ""Pause"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000108"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": ""Press"", ""initialStateCheck"": false }
      ],
      ""bindings"": [
        { ""name"": ""WASD"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000201"", ""path"": ""2DVector"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": true, ""isPartOfComposite"": false },
        { ""name"": ""up"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000202"", ""path"": ""<Keyboard>/w"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
        { ""name"": ""down"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000203"", ""path"": ""<Keyboard>/s"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
        { ""name"": ""left"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000204"", ""path"": ""<Keyboard>/a"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
        { ""name"": ""right"", ""id"": ""a1b2c3d4-0000-4000-8000-000000000205"", ""path"": ""<Keyboard>/d"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-000000000206"", ""path"": ""<Mouse>/delta"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Look"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-000000000207"", ""path"": ""<Keyboard>/leftShift"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Sprint"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-000000000208"", ""path"": ""<Keyboard>/leftCtrl"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Crouch"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-000000000209"", ""path"": ""<Keyboard>/space"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Jump"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-00000000020a"", ""path"": ""<Keyboard>/e"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Interact"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-00000000020b"", ""path"": ""<Keyboard>/f"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Flashlight"", ""isComposite"": false, ""isPartOfComposite"": false },
        { ""name"": """", ""id"": ""a1b2c3d4-0000-4000-8000-00000000020c"", ""path"": ""<Keyboard>/escape"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Pause"", ""isComposite"": false, ""isPartOfComposite"": false }
      ]
    }
  ],
  ""controlSchemes"": []
}";
    }
}
