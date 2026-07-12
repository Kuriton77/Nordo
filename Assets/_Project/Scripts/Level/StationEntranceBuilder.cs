using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nordo.Noise;
using Nordo.Lighting;
using Nordo.Enemy;
using Nordo.Interaction;
using Nordo.Items;
using Nordo.Progression;
using Nordo.Rendering;
using Nordo.UI;

namespace Nordo.Level
{
    /// <summary>
    /// Builds the first complete section of Vardø-9 at runtime and wires its full gameplay progression:
    /// entry → hall → offices/storage → the locked control room → the powered transmitter exit. The
    /// player collects a key, finds a fuse, powers the fuse box, starts the generator (loud — it draws
    /// The Listener), restores power (lights wake, the exit unlocks), and escapes. Environmental notes
    /// carry the story and clues.
    /// <para>
    /// Everything obeys the LOCKED art direction (docs/ART_DIRECTION.md): low-poly greybox, the
    /// <c>Nordo/PSX</c> shader from the <see cref="PSXPalette"/>, heavy cold fog via
    /// <see cref="SceneAtmosphere"/>, and few, hard-shadowed lights. Doors carve the NavMesh so a locked
    /// room genuinely keeps the hunter out until it is opened.
    /// </para>
    /// <para>Requires a player rig (Milestones 1–4) tagged <c>Player</c> with a CharacterController.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationEntranceBuilder : MonoBehaviour
    {
        [SerializeField] private bool _buildOnStart = true;
        [SerializeField] private string _playerTag = "Player";

        private const float WallH = 3.2f;
        private const float T = 0.3f;
        private static readonly Vector3 Spawn = new Vector3(0f, 1f, -9f);

        private Material _floorMat, _wallMat, _propMat, _doorMat, _noteMat, _enemyMat;
        private readonly List<GameObject> _doorLeaves = new();

        private void Start()
        {
            if (_buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Build Station Section")]
        public void Build()
        {
            StationDirector director = EnsureSystems(out ObjectiveTracker tracker);
            CreateMaterials();
            var root = new GameObject("Vardo9_Section").transform;

            BuildShell(root);

            // --- Doors (control = key-locked, exit = powered) ---
            var control = BuildDoor(root, new Vector3(-1.5f, 0f, 5f), 3f, WallH * 0.96f, "ControlDoor");
            control.Lockable.Configure(isLocked: true, requiredKeyId: "entry_key");

            var exit = BuildDoor(root, new Vector3(-1.5f, 0f, 13f), 3f, WallH * 0.96f, "ExitDoor");
            exit.Lockable.Configure(isLocked: true, requiredKeyId: string.Empty); // opened by power, not a key

            // --- Power grid + powered devices ---
            var power = new GameObject("PowerMain").AddComponent<PowerController>();
            power.transform.SetParent(root, false);

            var exitPowerLock = exit.Pivot.AddComponent<PoweredLock>();
            exitPowerLock.SetLock(exit.Lockable);
            exitPowerLock.SetGridId("main");

            // Always-on weak light so the entry is faintly readable; the rest wakes with power.
            MakeLight(root, new Vector3(0f, 3f, -9f), 8f, 0.5f, new Color(0.55f, 0.62f, 0.7f), enabled: true);
            MakePoweredLight(root, new Vector3(0f, 3f, 0f), 10f, 1.0f);   // hall
            MakePoweredLight(root, new Vector3(0f, 3f, 10f), 9f, 1.1f);   // control room
            MakePoweredLight(root, new Vector3(0f, 3f, 16f), 8f, 1.0f);   // transmitter

            // --- Items ---
            ItemDefinition keyDef = ItemDefinition.CreateRuntime(
                "entry_key", "Control Room Key", "A cold brass key, tagged 'CTRL'. The metal bites your fingers.", ItemCategory.Key);
            ItemDefinition fuseDef = ItemDefinition.CreateRuntime(
                "fuse", "Ceramic Fuse", "A 30-amp ceramic fuse. Heavier than it looks, and faintly warm.", ItemCategory.Fuse);

            MakeBox("Desk", root, new Vector3(-9f, 0.5f, 0f), new Vector3(1.6f, 1f, 0.9f), _propMat);
            MakePickup(root, new Vector3(-9f, 1.15f, 0f), keyDef);           // office
            MakeBox("Shelf", root, new Vector3(9f, 0.6f, 0f), new Vector3(0.9f, 1.2f, 1.6f), _propMat);
            MakePickup(root, new Vector3(9f, 1.4f, 0f), fuseDef);            // storage

            // --- Puzzle machines (in the control room) ---
            FuseBox fuseBox = MakeFuseBox(root, new Vector3(-3.6f, 1.4f, 9f));
            (Generator generator, MachineNoiseEmitter machine) = MakeGenerator(root, new Vector3(3f, 0.9f, 10f), fuseBox);

            // --- Environmental storytelling ---
            MakeNote(root, new Vector3(-11f, 1.25f, 2.4f), "note_intake",
                "Intake Log — 3 weeks ago",
                "Transmitter's dead again. Fed a fresh fuse into the board but the genny won't kick without it. " +
                "Key to the control room's in my desk. Whatever you do, keep the noise down after dark. It comes when you're loud.");
            MakeNote(root, new Vector3(0f, 1.25f, 3.6f), "note_hall",
                "Scrawled on the wall",
                "IT HEARS. IT DOESN'T SEE. STAY QUIET. STAY SLOW.");
            MakeNote(root, new Vector3(2.4f, 1.25f, 12f), "note_control",
                "Note taped to the generator",
                "Fuse in the box first, THEN the starter. She roars loud enough to wake the whole coast. " +
                "Once power's up the far door releases. Run for the relay — and pray it's already moved on.");

            // --- Objective chain ---
            tracker.SetObjectives(new List<ObjectiveStep>
            {
                new ObjectiveStep("reach_control", "Find a way into the control room.",
                    "The heavy door is locked. Someone left a key in one of the offices."),
                new ObjectiveStep("restore_power", "Restore power: fit a fuse, then start the generator.",
                    "The board is dead. A fuse should be in the storage room."),
                new ObjectiveStep("reach_exit", "Reach the transmitter room and get out.",
                    "Power's back — the far door should have released. But the generator was LOUD."),
            });

            // --- Wire the puzzle dependencies to progression + power ---
            control.Lockable.Unlocked += () => tracker.CompleteObjective("reach_control");
            generator.Started += () =>
            {
                power.RestorePower();
                tracker.CompleteObjective("restore_power");
            };

            // --- Exit trigger ---
            GameObject exitZone = MakeBox("ExitTrigger", root, new Vector3(0f, 1.2f, 16.5f), new Vector3(4f, 2.4f, 4f), _floorMat);
            Destroy(exitZone.GetComponent<MeshRenderer>()); // collider-only, and excluded from the NavMesh bake
            Destroy(exitZone.GetComponent<MeshFilter>());
            var exitCol = exitZone.GetComponent<BoxCollider>();
            exitCol.isTrigger = true;
            var section = exitZone.AddComponent<SectionExit>();
            section.Reached += () =>
            {
                tracker.CompleteObjective("reach_exit");
                director.OnSectionComplete();
            };

            // --- Player first (so it can be moved to spawn and kept out of the bake) ---
            Transform player = SetupPlayer();
            CharacterController playerCc = player != null ? player.GetComponent<CharacterController>() : null;

            // --- Bake NavMesh (doors carve dynamically; exclude the player), then place The Listener ---
            if (playerCc != null) playerCc.enabled = false;
            BakeNavMesh(root);
            if (playerCc != null) playerCc.enabled = true;

            ListenerController enemy = BuildEnemy(root);
            director.Configure(player, Spawn, enemy);

            Debug.Log("[Vardo9] Section built. Objective: restore power and reach the transmitter — quietly.");
        }

        // --- Systems ---------------------------------------------------------------

        private StationDirector EnsureSystems(out ObjectiveTracker tracker)
        {
            if (Object.FindObjectOfType<NoiseSystem>() == null) new GameObject("NoiseSystem").AddComponent<NoiseSystem>();
            if (Object.FindObjectOfType<LightVisibilitySystem>() == null) new GameObject("LightVisibilitySystem").AddComponent<LightVisibilitySystem>();
            if (Object.FindObjectOfType<SceneAtmosphere>() == null) new GameObject("SceneAtmosphere").AddComponent<SceneAtmosphere>();
            if (Object.FindObjectOfType<InventoryService>() == null) new GameObject("Inventory").AddComponent<InventoryService>();

            tracker = Object.FindObjectOfType<ObjectiveTracker>();
            if (tracker == null) tracker = new GameObject("Objectives").AddComponent<ObjectiveTracker>();

            if (Object.FindObjectOfType<StationHUD>() == null) new GameObject("StationHUD").AddComponent<StationHUD>();

            StationDirector director = Object.FindObjectOfType<StationDirector>();
            if (director == null) director = new GameObject("StationDirector").AddComponent<StationDirector>();
            return director;
        }

        // --- Geometry --------------------------------------------------------------

        private void BuildShell(Transform root)
        {
            MakeBox("Floor", root, new Vector3(0f, -0.5f, 3.5f), new Vector3(26f, 1f, 33f), _floorMat);
            MakeBox("Ceiling", root, new Vector3(0f, WallH, 3.5f), new Vector3(26f, 0.3f, 33f), _wallMat);

            // Hall (with doorways to every room).
            HWallGap(root, -5f, -5f, 5f, 0f, 1.5f);   // south → entry
            HWallGap(root, 5f, -5f, 5f, 0f, 1.5f);    // north → control (control door)
            VWallGap(root, -5f, -5f, 5f, 0f, 1.5f);   // west → office
            VWallGap(root, 5f, -5f, 5f, 0f, 1.5f);    // east → storage

            // Entry.
            HWall(root, -12f, -3f, 3f);
            VWall(root, -3f, -12f, -5f);
            VWall(root, 3f, -12f, -5f);

            // Office (west).
            VWall(root, -12f, -3f, 3f);
            HWall(root, 3f, -12f, -5f);
            HWall(root, -3f, -12f, -5f);

            // Storage (east).
            VWall(root, 12f, -3f, 3f);
            HWall(root, 3f, 5f, 12f);
            HWall(root, -3f, 5f, 12f);

            // Control room (north).
            VWall(root, -4f, 5f, 13f);
            VWall(root, 4f, 5f, 13f);
            HWallGap(root, 13f, -4f, 4f, 0f, 1.5f);   // north → transmitter (exit door)

            // Transmitter (far north).
            VWall(root, -3f, 13f, 19f);
            VWall(root, 3f, 13f, 19f);
            HWall(root, 19f, -3f, 3f);
        }

        private void HWall(Transform root, float z, float x0, float x1)
        {
            float len = Mathf.Abs(x1 - x0);
            if (len < 0.01f) return;
            MakeBox("Wall", root, new Vector3((x0 + x1) * 0.5f, WallH * 0.5f, z), new Vector3(len, WallH, T), _wallMat);
        }

        private void VWall(Transform root, float x, float z0, float z1)
        {
            float len = Mathf.Abs(z1 - z0);
            if (len < 0.01f) return;
            MakeBox("Wall", root, new Vector3(x, WallH * 0.5f, (z0 + z1) * 0.5f), new Vector3(T, WallH, len), _wallMat);
        }

        private void HWallGap(Transform root, float z, float x0, float x1, float gapC, float gapH)
        {
            HWall(root, z, x0, gapC - gapH);
            HWall(root, z, gapC + gapH, x1);
        }

        private void VWallGap(Transform root, float x, float z0, float z1, float gapC, float gapH)
        {
            VWall(root, x, z0, gapC - gapH);
            VWall(root, x, gapC + gapH, z1);
        }

        private struct DoorRefs { public GameObject Pivot; public Lockable Lockable; }

        private DoorRefs BuildDoor(Transform root, Vector3 hinge, float width, float height, string name)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(root, false);
            pivot.transform.position = new Vector3(hinge.x, height * 0.5f, hinge.z);

            GameObject leaf = MakeBox(name + "_Leaf", pivot.transform,
                pivot.transform.position + new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(width, height, 0.14f), _doorMat);

            // Carving obstacle: a closed door blocks the NavMesh; swinging it open clears the doorway.
            var obstacle = leaf.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;

            pivot.AddComponent<AudioSource>();
            pivot.AddComponent<Highlighter>();
            var lockable = pivot.AddComponent<Lockable>(); // added before Door so Door.Awake sees it
            pivot.AddComponent<Door>();

            _doorLeaves.Add(leaf);
            return new DoorRefs { Pivot = pivot, Lockable = lockable };
        }

        // --- Props / puzzles -------------------------------------------------------

        private void MakePickup(Transform root, Vector3 pos, ItemDefinition def)
        {
            GameObject go = MakeBox("Pickup_" + def.Id, root, pos, new Vector3(0.26f, 0.16f, 0.5f), _propMat);
            go.AddComponent<Highlighter>();
            go.AddComponent<ItemPickup>().SetItem(def, 1);
        }

        private void MakeNote(Transform root, Vector3 pos, string id, string title, string body)
        {
            GameObject go = MakeBox("Note_" + id, root, pos, new Vector3(0.32f, 0.42f, 0.03f), _noteMat);
            go.AddComponent<Highlighter>();
            go.AddComponent<ReadableNote>().SetNote(id, title, body);
        }

        private FuseBox MakeFuseBox(Transform root, Vector3 pos)
        {
            GameObject go = MakeBox("FuseBox", root, pos, new Vector3(0.6f, 0.8f, 0.25f), _propMat);
            go.AddComponent<Highlighter>();
            FuseBox fb = go.AddComponent<FuseBox>();
            fb.SetRequiredFuse("fuse");
            return fb;
        }

        private (Generator, MachineNoiseEmitter) MakeGenerator(Transform root, Vector3 pos, FuseBox prerequisite)
        {
            GameObject go = MakeBox("Generator", root, pos, new Vector3(1.3f, 1.5f, 1.1f), _propMat);
            go.AddComponent<Highlighter>();
            var machine = go.AddComponent<MachineNoiseEmitter>();
            Generator gen = go.AddComponent<Generator>();
            gen.Configure(prerequisite, machine);
            return (gen, machine);
        }

        private void MakePoweredLight(Transform root, Vector3 pos, float range, float intensity)
        {
            var go = new GameObject("PoweredLight");
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(0.7f, 0.78f, 0.85f);
            light.shadows = LightShadows.Hard;
            light.enabled = false;

            go.AddComponent<PoweredLight>().SetGridId("main"); // Awake picks up the Light on this object
        }

        private static Light MakeLight(Transform root, Vector3 pos, float range, float intensity, Color color, bool enabled)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Hard;
            light.enabled = enabled;
            return light;
        }

        // --- NavMesh ---------------------------------------------------------------

        private void BakeNavMesh(Transform root)
        {
            for (int i = 0; i < _doorLeaves.Count; i++)
            {
                if (_doorLeaves[i] != null) _doorLeaves[i].SetActive(false);
            }

            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            for (int i = 0; i < _doorLeaves.Count; i++)
            {
                if (_doorLeaves[i] != null) _doorLeaves[i].SetActive(true);
            }
        }

        // --- Enemy -----------------------------------------------------------------

        private ListenerController BuildEnemy(Transform root)
        {
            GameObject enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyGo.name = "TheListener";
            enemyGo.transform.SetParent(root, false);
            Destroy(enemyGo.GetComponent<Collider>());
            enemyGo.GetComponent<MeshRenderer>().sharedMaterial = _enemyMat;

            Vector3 start = new Vector3(0f, 1f, 0f);
            if (NavMesh.SamplePosition(start, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                start = hit.position;
            }
            enemyGo.transform.position = start + Vector3.up;

            var routeGo = new GameObject("ListenerPatrol");
            routeGo.transform.SetParent(root, false);
            Vector3[] points =
            {
                new Vector3(3f, 0f, 0f), new Vector3(-3f, 0f, 0f),
                new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -3f), new Vector3(4f, 0f, 1f)
            };
            var waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(routeGo.transform, false);
                wp.transform.position = points[i] + Vector3.up;
                waypoints[i] = wp.transform;
            }
            var route = routeGo.AddComponent<PatrolRoute>();
            route.SetWaypoints(waypoints);

            enemyGo.AddComponent<ListenerAudio>();
            enemyGo.AddComponent<ListenerAnimator>();
            ListenerController controller = enemyGo.AddComponent<ListenerController>();
            controller.AssignPatrol(route);
            controller.Agent.baseOffset = 1f;
            return controller;
        }

        // --- Player ----------------------------------------------------------------

        private Transform SetupPlayer()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerGo == null)
            {
                Debug.LogWarning("[Vardo9] No 'Player'-tagged rig found. Add your player (Milestones 1–4) " +
                                 "tagged 'Player' with a CharacterController, then rebuild. The section is otherwise live.");
                return null;
            }

            if (playerGo.GetComponent<PlayerNoiseEmitter>() == null)
            {
                playerGo.AddComponent<PlayerNoiseEmitter>();
            }

            var cc = playerGo.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                playerGo.transform.position = Spawn;
                cc.enabled = true;
            }
            else
            {
                playerGo.transform.position = Spawn;
            }

            return playerGo.transform;
        }

        // --- Materials / helpers ---------------------------------------------------

        private void CreateMaterials()
        {
            var p = ScriptableObject.CreateInstance<PSXPalette>();
            _floorMat = MakeMaterial(p.Slate);
            _wallMat = MakeMaterial(p.DeepSteel);
            _propMat = MakeMaterial(p.ColdGrey);
            _doorMat = MakeMaterial(Color.Lerp(p.DeepSteel, p.ColdGrey, 0.5f));
            _noteMat = MakeMaterial(p.IceHighlight);
            _enemyMat = MakeMaterial(p.Void);
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Nordo/PSX");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            return material;
        }

        private static GameObject MakeBox(string name, Transform parent, Vector3 center, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return go;
        }
    }
}
