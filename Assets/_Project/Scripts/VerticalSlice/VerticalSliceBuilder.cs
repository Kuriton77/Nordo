using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nordo.Core;
using Nordo.Noise;
using Nordo.Lighting;
using Nordo.Enemy;
using Nordo.Interaction;
using Nordo.Rendering;

namespace Nordo.VerticalSlice
{
    /// <summary>
    /// Builds a small, fully playable test area for The Listener entirely at runtime — floor, rooms, a
    /// door, throwable props, a hiding nook, a patrolling Listener, an exit, and the objective HUD —
    /// then bakes a NavMesh and wires the director. This avoids fragile hand-authored scene files: drop
    /// this one component on an empty object in an empty scene, press Play, and the slice assembles.
    /// <para>
    /// <b>Requirements:</b> a player rig (from Milestones 1–4) present in the scene and tagged
    /// <c>Player</c> with a <c>CharacterController</c>. The builder finds it, ensures a
    /// <c>PlayerNoiseEmitter</c>, and teleports it to the spawn point. Everything else is generated.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerticalSliceBuilder : MonoBehaviour
    {
        [Tooltip("Build automatically when entering Play mode.")]
        [SerializeField] private bool _buildOnStart = true;

        [Tooltip("Tag used to locate the player rig in the scene.")]
        [SerializeField] private string _playerTag = "Player";

        private Material _floorMat;
        private Material _wallMat;
        private Material _propMat;
        private Material _doorMat;
        private Material _enemyMat;
        private Material _exitMat;

        private static readonly Vector3 SpawnPoint = new Vector3(-8f, 1f, -8f);
        private static readonly Vector3 EnemyStart = new Vector3(6f, 1f, 6f);

        private void Start()
        {
            if (_buildOnStart)
            {
                Build();
            }
        }

        /// <summary>Constructs the entire test area.</summary>
        [ContextMenu("Build Slice")]
        public void Build()
        {
            EnsureSystems();
            CreateMaterials();

            var root = new GameObject("VerticalSlice").transform;

            List<GameObject> doors = BuildEnvironment(root);
            BakeNavMesh(root, doors);
            BuildProps(root);
            ListenerController enemy = BuildEnemy(root);
            ExitZone exit = BuildExit(root);
            Transform player = SetupPlayer();

            var director = new GameObject("SliceDirector").AddComponent<VerticalSliceDirector>();
            director.Configure(player, SpawnPoint, enemy);
            exit.Reached += director.OnPlayerEscaped;

            Debug.Log("[VerticalSlice] Test area built. Objective: reach the EXIT while staying quiet.");
        }

        // --- Systems ----------------------------------------------------------------

        private static void EnsureSystems()
        {
            if (Object.FindObjectOfType<NoiseSystem>() == null)
            {
                new GameObject("NoiseSystem").AddComponent<NoiseSystem>();
            }

            if (Object.FindObjectOfType<LightVisibilitySystem>() == null)
            {
                new GameObject("LightVisibilitySystem").AddComponent<LightVisibilitySystem>();
            }

            // Enforce the locked art direction: heavy cold fog + dim ambient (docs/ART_DIRECTION.md).
            if (Object.FindObjectOfType<SceneAtmosphere>() == null)
            {
                new GameObject("SceneAtmosphere").AddComponent<SceneAtmosphere>();
            }
        }

        // --- Environment ------------------------------------------------------------

        private List<GameObject> BuildEnvironment(Transform root)
        {
            const float half = 12f;
            const float wallH = 3.2f;
            const float t = 0.3f;

            // Floor.
            CreateBox("Floor", root, new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f, 1f, half * 2f), _floorMat);

            // Outer walls.
            CreateBox("Wall_N", root, new Vector3(0f, wallH * 0.5f, half), new Vector3(half * 2f, wallH, t), _wallMat);
            CreateBox("Wall_S", root, new Vector3(0f, wallH * 0.5f, -half), new Vector3(half * 2f, wallH, t), _wallMat);
            CreateBox("Wall_E", root, new Vector3(half, wallH * 0.5f, 0f), new Vector3(t, wallH, half * 2f), _wallMat);
            CreateBox("Wall_W", root, new Vector3(-half, wallH * 0.5f, 0f), new Vector3(t, wallH, half * 2f), _wallMat);

            // Interior dividing wall at z = 0 with a 2 m doorway gap around x = 0.
            const float gap = 1f; // half-width of doorway
            float segLen = half - gap;
            CreateBox("Divider_Left", root, new Vector3(-(gap + segLen * 0.5f), wallH * 0.5f, 0f), new Vector3(segLen, wallH, t), _wallMat);
            CreateBox("Divider_Right", root, new Vector3(gap + segLen * 0.5f, wallH * 0.5f, 0f), new Vector3(segLen, wallH, t), _wallMat);

            // A door filling the gap (hinged at the left edge).
            GameObject door = BuildDoor(root, new Vector3(-gap, 0f, 0f), gap * 2f, wallH * 0.95f);

            // Hiding nook in the south-west (an L of short walls to duck behind).
            CreateBox("Nook_A", root, new Vector3(-9.5f, wallH * 0.4f, -6f), new Vector3(t, wallH * 0.8f, 4f), _wallMat);
            CreateBox("Nook_B", root, new Vector3(-7.7f, wallH * 0.4f, -4.2f), new Vector3(3.6f, wallH * 0.8f, t), _wallMat);

            return new List<GameObject> { door };
        }

        private GameObject BuildDoor(Transform root, Vector3 hingePosition, float width, float height)
        {
            // Pivot carries the Door component (hinge defaults to its own transform).
            var pivot = new GameObject("Door");
            pivot.transform.SetParent(root, false);
            pivot.transform.position = new Vector3(hingePosition.x, height * 0.5f, hingePosition.z);

            // Leaf is a child offset by half its width so it swings about the hinge edge.
            GameObject leaf = CreateBox("DoorLeaf", pivot.transform,
                pivot.transform.position + new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(width, height, 0.12f), _doorMat);

            pivot.AddComponent<AudioSource>();
            pivot.AddComponent<Highlighter>();
            pivot.AddComponent<Door>(); // IAutoDoor; unlocked → the enemy can push it open

            return leaf; // returned so it can be disabled during the NavMesh bake
        }

        // --- NavMesh ----------------------------------------------------------------

        private static void BakeNavMesh(Transform root, List<GameObject> doors)
        {
            // Disable door leaves so the doorway is treated as open ground in the bake — the enemy can
            // then path through freely and simply opens the visual door as it arrives.
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] != null)
                {
                    doors[i].SetActive(false);
                }
            }

            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] != null)
                {
                    doors[i].SetActive(true);
                }
            }
        }

        // --- Props ------------------------------------------------------------------

        private void BuildProps(Transform root)
        {
            // A table in room A with three throwable bottles/cans (cubes) to distract with.
            GameObject table = CreateBox("Table", root, new Vector3(-4f, 0.5f, -4f), new Vector3(1.6f, 1f, 1f), _propMat);
            table.isStatic = true;

            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = new Vector3(-4.4f + i * 0.4f, 1.2f, -4f);
                GameObject prop = CreateBox($"Throwable_{i}", root, pos, new Vector3(0.22f, 0.22f, 0.22f), _propMat);

                var body = prop.AddComponent<Rigidbody>();
                body.mass = 0.6f;

                prop.AddComponent<Grabbable>();
                prop.AddComponent<ImpactNoiseEmitter>();
            }
        }

        // --- Enemy ------------------------------------------------------------------

        private ListenerController BuildEnemy(Transform root)
        {
            // Visible capsule with its physics collider removed (the NavMeshAgent drives movement).
            GameObject enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyGo.name = "TheListener";
            enemyGo.transform.SetParent(root, false);
            Destroy(enemyGo.GetComponent<Collider>());
            enemyGo.GetComponent<MeshRenderer>().sharedMaterial = _enemyMat;

            // Snap the spawn to the navmesh.
            Vector3 start = EnemyStart;
            if (NavMesh.SamplePosition(EnemyStart, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                start = hit.position;
            }
            enemyGo.transform.position = start + Vector3.up;

            // Patrol route (waypoints are world-static children of the route object).
            var routeGo = new GameObject("ListenerPatrol");
            routeGo.transform.SetParent(root, false);
            Vector3[] points =
            {
                new Vector3(8f, 0f, 4f), new Vector3(-6f, 0f, 8f),
                new Vector3(0f, 0f, 9f), new Vector3(8f, 0f, 10f)
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
            ListenerController controller = enemyGo.AddComponent<ListenerController>(); // adds NavMeshAgent
            controller.AssignPatrol(route);

            // The capsule's pivot is its centre, so lift the agent by half its height to sit on the floor.
            controller.Agent.baseOffset = 1f;

            return controller;
        }

        // --- Exit -------------------------------------------------------------------

        private ExitZone BuildExit(Transform root)
        {
            GameObject exitGo = CreateBox("Exit", root, new Vector3(9f, 1.1f, 10f), new Vector3(2.5f, 2.2f, 2.5f), _exitMat);
            var collider = exitGo.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            return exitGo.AddComponent<ExitZone>();
        }

        // --- Player -----------------------------------------------------------------

        private Transform SetupPlayer()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerGo == null)
            {
                Debug.LogWarning($"[VerticalSlice] No GameObject tagged '{_playerTag}' found. Add your " +
                                 "player rig (Milestones 1–4) tagged 'Player' with a CharacterController, " +
                                 "then rebuild. The level and enemy are still active.");
                return null;
            }

            // Ensure player noise reaches The Listener.
            if (playerGo.GetComponent<PlayerNoiseEmitter>() == null)
            {
                playerGo.AddComponent<PlayerNoiseEmitter>();
            }

            // Place the player at spawn.
            var controller = playerGo.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                playerGo.transform.position = SpawnPoint;
                controller.enabled = true;
            }
            else
            {
                playerGo.transform.position = SpawnPoint;
            }

            return playerGo.transform;
        }

        // --- Helpers ----------------------------------------------------------------

        private void CreateMaterials()
        {
            // Colours come from the LOCKED art-direction palette (docs/ART_DIRECTION.md, §3).
            var p = ScriptableObject.CreateInstance<PSXPalette>();

            _floorMat = MakeMaterial(p.Slate);
            _wallMat = MakeMaterial(p.DeepSteel);
            _propMat = MakeMaterial(p.ColdGrey);
            _doorMat = MakeMaterial(Color.Lerp(p.DeepSteel, p.ColdGrey, 0.5f));
            _enemyMat = MakeMaterial(p.Void);
            _exitMat = MakeMaterial(p.SicklyTeal); // a cold teal "safe" signal, still in-palette
        }

        // Prefers the locked Nordo/PSX shader; falls back gracefully if it hasn't imported yet.
        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Nordo/PSX");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            material.color = color;
            return material;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 center, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }
    }
}
