using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Nordo.Core;
using Nordo.Audio;
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
    /// Builds Vardø-9: twelve hand-designed rooms — airlock, entrance, main hall, kitchen, sleeping
    /// quarters (barricaded), bathroom, storage, frozen utility store (broken window), maintenance
    /// corridor (red emergency light), electrical room, generator room and the radio room — dressed
    /// with PSX-textured props, environmental storytelling (blood trail, abandoned meals, a barricade
    /// that held and didn't matter), a full procedural soundscape, and the key → fuse → generator →
    /// power → signal progression, all hunted by The Listener.
    /// <para>
    /// Everything reuses the existing systems: interaction, progression, noise, lighting, enemy. This
    /// class is level design and set dressing, not framework.
    /// </para>
    /// <para>Requires a player rig (built by <c>GameBootstrap</c>) tagged <c>Player</c>.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationEntranceBuilder : MonoBehaviour
    {
        [SerializeField] private bool _buildOnStart = true;
        [SerializeField] private string _playerTag = "Player";

        private const float WallH = 3.4f;
        private const float T = 0.3f;
        private static readonly Vector3 Spawn = new Vector3(0f, 1f, -16f);

        private StationPropKit.MaterialSet _m;
        private readonly List<GameObject> _doorLeaves = new();
        private readonly List<PoweredLight> _poweredLights = new();

        private void Start()
        {
            if (_buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Build Station")]
        public void Build()
        {
            StationDirector director = EnsureSystems(out ObjectiveTracker tracker);
            _m = StationPropKit.CreateMaterials();
            Transform root = new GameObject("Vardo9").transform;

            BuildArchitecture(root);
            var doors = BuildDoors(root);
            BuildLighting(root);
            DressRooms(root);
            BuildAmbience(root);
            BuildProgression(root, tracker, doors, director);

            // Player first (kept out of the bake), then the mesh, then the hunter.
            Transform player = SetupPlayer();
            CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
            if (cc != null) cc.enabled = false;
            BakeNavMesh(root);
            if (cc != null) cc.enabled = true;

            BuildThrowables(root);
            ListenerController enemy = BuildEnemy(root);
            director.Configure(player, Spawn, enemy);

            Debug.Log("[Vardo9] Station built. Stay quiet. Restore power. Send the signal.");
        }

        // ================================================================ systems

        private StationDirector EnsureSystems(out ObjectiveTracker tracker)
        {
            if (FindObjectOfType<NoiseSystem>() == null) new GameObject("NoiseSystem").AddComponent<NoiseSystem>();
            if (FindObjectOfType<LightVisibilitySystem>() == null) new GameObject("LightVisibilitySystem").AddComponent<LightVisibilitySystem>();
            if (FindObjectOfType<SceneAtmosphere>() == null) new GameObject("SceneAtmosphere").AddComponent<SceneAtmosphere>();
            if (FindObjectOfType<InventoryService>() == null) new GameObject("Inventory").AddComponent<InventoryService>();
            tracker = FindObjectOfType<ObjectiveTracker>();
            if (tracker == null) tracker = new GameObject("Objectives").AddComponent<ObjectiveTracker>();
            if (FindObjectOfType<StationHUD>() == null) new GameObject("StationHUD").AddComponent<StationHUD>();
            StationDirector director = FindObjectOfType<StationDirector>();
            if (director == null) director = new GameObject("StationDirector").AddComponent<StationDirector>();
            return director;
        }

        // ================================================================ architecture

        private void BuildArchitecture(Transform root)
        {
            var a = new GameObject("Architecture").transform;
            a.SetParent(root, false);

            // Ground slab under everything, and per-room ceilings at varied heights (claustrophobia
            // is a dial: hall breathes at 3.3, the corridor presses down at 2.4).
            StationPropKit.Box(a, "Floor", new Vector3(0, -0.5f, 0), new Vector3(34, 1, 38), _m.Floor);
            Ceiling(a, new Vector3(0, 2.5f, -16), new Vector2(4, 4));       // airlock
            Ceiling(a, new Vector3(0, 2.9f, -11), new Vector2(12, 6));      // entrance
            Ceiling(a, new Vector3(0, 3.3f, -2), new Vector2(12, 12));      // hall
            Ceiling(a, new Vector3(-11, 2.9f, -5), new Vector2(10, 6));     // kitchen
            Ceiling(a, new Vector3(-11, 2.9f, 1), new Vector2(10, 6));      // quarters
            Ceiling(a, new Vector3(-13.5f, 2.4f, 7), new Vector2(5, 6));    // bathroom
            Ceiling(a, new Vector3(-4.5f, 2.4f, 7), new Vector2(13, 6));    // corridor
            Ceiling(a, new Vector3(4, 2.7f, 7), new Vector2(4, 6));         // electrical
            Ceiling(a, new Vector3(11, 3.0f, -5), new Vector2(10, 6));      // storage
            Ceiling(a, new Vector3(11, 3.0f, 2), new Vector2(10, 8));       // utility
            Ceiling(a, new Vector3(-2, 3.3f, 14), new Vector2(8, 8));       // generator
            Ceiling(a, new Vector3(6, 2.9f, 14), new Vector2(8, 8));        // radio

            // Metal floors where the station works for a living (also switches footstep audio).
            SurfaceDefinition metal = SurfaceDefinition.CreateRuntime(
                SurfaceKind.Metal, ProceduralAudio.FootstepSet(metal: true),
                new[] { ProceduralAudio.LandThud() }, noiseLoudness: 0.75f, baseVolume: 0.6f);
            MetalOverlay(a, new Vector3(-4.5f, 0.02f, 7), new Vector2(13, 6), metal);   // corridor
            MetalOverlay(a, new Vector3(4, 0.02f, 7), new Vector2(4, 6), metal);        // electrical
            StationPropKit.Box(a, "IceFloor", new Vector3(11, 0.02f, 2), new Vector3(10, 0.04f, 8), _m.IceFloor); // utility

            // ---- Airlock (sealed outer door: the way out that isn't) ----
            VWall(a, -2, -18, -14); VWall(a, 2, -18, -14);
            StationPropKit.Box(a, "OuterDoor", new Vector3(0, WallH * 0.5f, -18), new Vector3(4, WallH, T), _m.SealedDoor);
            HWallGap(a, -14, -2, 2, 0, 0.9f); Header(a, new Vector3(0, 0, -14), 1.8f, false);

            // ---- Entrance ----
            HWall(a, -14, -6, -2); HWall(a, -14, 2, 6);
            VWall(a, -6, -14, -8); VWall(a, 6, -14, -8);
            HWallGap(a, -8, -6, 6, 0, 1.1f); Header(a, new Vector3(0, 0, -8), 2.2f, false, 2.5f);

            // ---- Hall perimeter ----
            VWallGap(a, -6, -8, 4, -5, 0.9f); Header(a, new Vector3(-6, 0, -5), 1.8f, true);   // → kitchen
            VWallGap(a, 6, -8, 4, -5, 0.9f); Header(a, new Vector3(6, 0, -5), 1.8f, true);     // → storage
            HWallGap(a, 4, -6, 6, 0, 1.1f); Header(a, new Vector3(0, 0, 4), 2.2f, false, 2.5f); // → corridor

            // ---- West wing: kitchen / quarters / bathroom ----
            HWall(a, -8, -16, -6);
            VWall(a, -16, -8, 10);
            HWallGap(a, -2, -16, -6, -11, 0.9f); Header(a, new Vector3(-11, 0, -2), 1.8f, false);
            HWallGap(a, 4, -16, -6, -8, 0.9f); Header(a, new Vector3(-8, 0, 4), 1.8f, false);
            HWall(a, 10, -16, -11);
            VWallGap(a, -11, 4, 10, 7, 0.9f); Header(a, new Vector3(-11, 0, 7), 1.8f, true);

            // ---- Corridor north edge + generator/radio south walls ----
            HWall(a, 10, -11, -6);
            HWallGap(a, 10, -6, 2, -2, 0.9f); Header(a, new Vector3(-2, 0, 10), 1.8f, false);  // D3
            HWall(a, 10, 2, 10);

            // ---- Electrical room ----
            VWallGap(a, 2, 4, 10, 7, 0.9f); Header(a, new Vector3(2, 0, 7), 1.8f, true);       // D_elec
            VWall(a, 6, 4, 10);

            // ---- East wing: storage / utility ----
            HWall(a, -8, 6, 16);
            VWall(a, 16, -8, -2);
            HWallGap(a, -2, 6, 16, 11, 0.9f); Header(a, new Vector3(11, 0, -2), 1.8f, false);
            HWall(a, 6, 6, 16);
            // Utility east wall with the broken window (hole y 1.0–2.2, z 1–3).
            VWall(a, 16, -2, 1); VWall(a, 16, 3, 6);
            StationPropKit.Box(a, "WindowSill", new Vector3(16, 0.5f, 2), new Vector3(T, 1f, 2f), _m.Wall);
            StationPropKit.Box(a, "WindowHeader", new Vector3(16, 2.8f, 2), new Vector3(T, 1.2f, 2f), _m.Wall);

            // ---- North rooms ----
            VWall(a, -6, 10, 18); HWall(a, 18, -6, 10); VWall(a, 10, 10, 18);
            VWallGap(a, 2, 10, 18, 14, 0.9f); Header(a, new Vector3(2, 0, 14), 1.8f, true);    // D4

            // ---- Exterior: the world past the broken glass ----
            StationPropKit.Box(a, "SnowGround", new Vector3(19, 0.05f, 2), new Vector3(6, 0.1f, 12), _m.SnowMat);
            StationPropKit.SnowDrift(a, _m, new Vector3(18, 0.1f, 0.5f), 1.6f);
            StationPropKit.SnowDrift(a, _m, new Vector3(18.5f, 0.1f, 4f), 1.2f);
        }

        private void Ceiling(Transform p, Vector3 center, Vector2 size)
            => StationPropKit.Box(p, "Ceiling", center, new Vector3(size.x, 0.2f, size.y), _m.Wall);

        private void MetalOverlay(Transform p, Vector3 center, Vector2 size, SurfaceDefinition surface)
        {
            GameObject o = StationPropKit.Box(p, "MetalFloor", center, new Vector3(size.x, 0.04f, size.y), _m.Tread);
            o.AddComponent<SurfaceIdentifier>().SetSurface(surface);
        }

        private void HWall(Transform p, float z, float x0, float x1)
        {
            float len = Mathf.Abs(x1 - x0);
            if (len < 0.01f) return;
            StationPropKit.Box(p, "Wall", new Vector3((x0 + x1) * 0.5f, WallH * 0.5f, z), new Vector3(len, WallH, T), _m.Wall);
        }

        private void VWall(Transform p, float x, float z0, float z1)
        {
            float len = Mathf.Abs(z1 - z0);
            if (len < 0.01f) return;
            StationPropKit.Box(p, "Wall", new Vector3(x, WallH * 0.5f, (z0 + z1) * 0.5f), new Vector3(T, WallH, len), _m.Wall);
        }

        private void HWallGap(Transform p, float z, float x0, float x1, float gapC, float gapH)
        {
            HWall(p, z, x0, gapC - gapH);
            HWall(p, z, gapC + gapH, x1);
        }

        private void VWallGap(Transform p, float x, float z0, float z1, float gapC, float gapH)
        {
            VWall(p, x, z0, gapC - gapH);
            VWall(p, x, gapC + gapH, z1);
        }

        /// <summary>Fills the wall above a doorway so openings read as doorframes, not missing walls.</summary>
        private void Header(Transform p, Vector3 at, float width, bool alongZ, float doorTop = 2.2f)
        {
            float h = WallH - doorTop;
            Vector3 size = alongZ ? new Vector3(T, h, width) : new Vector3(width, h, T);
            StationPropKit.Box(p, "DoorHeader", new Vector3(at.x, doorTop + h * 0.5f, at.z), size, _m.Wall);
        }

        // ================================================================ doors

        private struct DoorRefs { public Door Door; public Lockable Lockable; }

        private Dictionary<string, DoorRefs> BuildDoors(Transform root)
        {
            var d = new Dictionary<string, DoorRefs>
            {
                ["airlock"] = MakeDoor(root, new Vector3(-0.9f, 0, -14), 1.8f, alongZ: false, "AirlockDoor", locked: false, keyId: ""),
                ["electrical"] = MakeDoor(root, new Vector3(2, 0, 6.1f), 1.8f, alongZ: true, "ElectricalDoor", locked: false, keyId: ""),
                ["generator"] = MakeDoor(root, new Vector3(-2.9f, 0, 10), 1.8f, alongZ: false, "GeneratorDoor", locked: true, keyId: "generator_key"),
                ["radio"] = MakeDoor(root, new Vector3(2, 0, 13.1f), 1.8f, alongZ: true, "RadioDoor", locked: true, keyId: "")
            };
            return d;
        }

        private DoorRefs MakeDoor(Transform root, Vector3 hinge, float width, bool alongZ, string name, bool locked, string keyId)
        {
            const float height = 2.2f;
            var pivot = new GameObject(name);
            pivot.transform.SetParent(root, false);
            pivot.transform.position = new Vector3(hinge.x, height * 0.5f, hinge.z);

            Vector3 offset = alongZ ? new Vector3(0, 0, width * 0.5f) : new Vector3(width * 0.5f, 0, 0);
            Vector3 size = alongZ ? new Vector3(0.12f, height, width) : new Vector3(width, height, 0.12f);
            GameObject leaf = StationPropKit.Box(pivot.transform, name + "_Leaf", pivot.transform.position + offset, size, _m.Door);

            var obstacle = leaf.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;

            var src = pivot.AddComponent<AudioSource>();
            ConfigureSource(src, null, loop: false, volume: 0.8f, maxDistance: 14f);
            pivot.AddComponent<Highlighter>();
            var lockable = pivot.AddComponent<Lockable>();
            var door = pivot.AddComponent<Door>();

            lockable.Configure(locked, keyId);
            lockable.ConfigureAudio(src, ProceduralAudio.LockedRattle(), ProceduralAudio.UnlockClunk());
            door.ConfigureAudio(src, ProceduralAudio.DoorOpen(), ProceduralAudio.DoorClose());

            _doorLeaves.Add(leaf);
            return new DoorRefs { Door = door, Lockable = lockable };
        }

        // ================================================================ lighting

        private void BuildLighting(Transform root)
        {
            var l = new GameObject("Lighting").transform;
            l.SetParent(root, false);

            Color cold = new Color(0.62f, 0.70f, 0.80f);
            Color red = new Color(0.62f, 0.14f, 0.10f);
            Color amber = new Color(0.78f, 0.47f, 0.12f);
            Color moon = new Color(0.45f, 0.60f, 0.85f);
            Color teal = new Color(0.28f, 0.55f, 0.50f);

            // Always-on islands of light in an otherwise dark station.
            PointLight(l, new Vector3(0, 2.2f, -16), cold, 0.6f, 5f, shadows: false);
            Light entry = PointLight(l, new Vector3(0, 2.6f, -11), cold, 0.75f, 6f, shadows: false);
            entry.gameObject.AddComponent<FlickeringLight>().Configure(entry, 0.3f, 8f, 0.08f);

            // The corridor's identity: two dying red emergency lamps.
            foreach (float x in new[] { -8f, -1f })
            {
                Light r = PointLight(l, new Vector3(x, 2.15f, 7), red, 0.9f, 5.5f, shadows: false);
                r.gameObject.AddComponent<FlickeringLight>().Configure(r, 0.4f, 11f, 0.2f);
                StationPropKit.Box(l, "RedLampFixture", new Vector3(x, 2.32f, 7), new Vector3(0.25f, 0.12f, 0.18f), _m.Rust, 0f, collider: false);
            }

            PointLight(l, new Vector3(-13.6f, 0.5f, 1.6f), amber, 0.55f, 4f, shadows: false);   // quarters lantern
            PointLight(l, new Vector3(17.2f, 2.3f, 2), moon, 1.3f, 9f, shadows: true);          // moonlight through the window
            PointLight(l, new Vector3(12, 2.3f, 2.2f), moon, 0.3f, 5f, shadows: false);         // utility spill
            PointLight(l, new Vector3(4.8f, 1.6f, 9.4f), amber, 0.3f, 2.5f, shadows: false);    // electrical pilot LED

            // Powered lights: dead until the generator runs — the payoff of the whole puzzle.
            PoweredPoint(l, new Vector3(0, 3.0f, -2), cold, 1.0f, 8.5f, shadows: true);          // hall
            PoweredPoint(l, new Vector3(-11, 2.6f, -5), cold, 0.85f, 6.5f, shadows: false);      // kitchen
            PoweredPoint(l, new Vector3(11, 2.7f, -5), cold, 0.85f, 6.5f, shadows: false);       // storage
            PoweredPoint(l, new Vector3(-2, 3.0f, 14), cold, 0.95f, 7.5f, shadows: true);        // generator
            PoweredPoint(l, new Vector3(6, 2.6f, 14), cold, 0.85f, 6.5f, shadows: false);        // radio
        }

        private Light PointLight(Transform p, Vector3 at, Color color, float intensity, float range, bool shadows)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(p, false);
            go.transform.position = at;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            return light;
        }

        private void PoweredPoint(Transform p, Vector3 at, Color color, float intensity, float range, bool shadows)
        {
            Light light = PointLight(p, at, color, intensity, range, shadows);
            light.enabled = false;
            var powered = light.gameObject.AddComponent<PoweredLight>();
            powered.SetGridId("main");
            _poweredLights.Add(powered);
            StationPropKit.Box(p, "LampFixture", at + new Vector3(0, 0.18f, 0), new Vector3(0.5f, 0.1f, 0.3f), _m.MetalWall, 0f, collider: false);
        }

        // ================================================================ set dressing (every room tells its part)

        private void DressRooms(Transform root)
        {
            var s = new GameObject("SetDressing").transform;
            s.SetParent(root, false);
            var k = _m;

            // AIRLOCK — the cold got in first.
            StationPropKit.FrostPatch(s, k, new Vector3(-1.83f, 1.4f, -16.5f), 90f, 1.2f);
            StationPropKit.FrostPatch(s, k, new Vector3(1.83f, 1.1f, -15.4f), 90f, 0.9f);
            StationPropKit.CrateStack(s, k, new Vector3(1.2f, 0, -17.2f));

            // ENTRANCE — coats still hanging; nobody left by the front door.
            StationPropKit.CoatRack(s, k, new Vector3(-4f, 0, -13.5f), 0f);
            StationPropKit.Bench(s, k, new Vector3(3.8f, 0, -12.6f), 0f);
            StationPropKit.CrateStack(s, k, new Vector3(-5f, 0, -9f));
            MakeNote(s, new Vector3(1.7f, 1.4f, -8.2f), 0f, "note_intake", "Duty Log — Halvorsen",
                "Week three with the transmitter dark. The main fuse blew when the fence went down, and the spares " +
                "are in the cold store — past the storage racks, where the window broke. Nobody volunteers.\n\n" +
                "New rules, posted twice: boots off after dark. No voices. EVER. The generator room stays locked — " +
                "key's in my locker in the bunk room. If it must run, count your steps and be gone before the echo dies.");

            // MAIN HALL — a meal interrupted, a message left in scratches, and the trail.
            StationPropKit.Table(s, k, new Vector3(1.5f, 0, -3f), 15f, overturned: true);
            StationPropKit.Chair(s, k, new Vector3(0.3f, 0, -4.2f), 200f, tipped: true);
            StationPropKit.Chair(s, k, new Vector3(2.8f, 0, -2f), 140f, tipped: false);
            StationPropKit.PaperScatter(s, k, new Vector3(1f, 0, -3.5f), 6, seed: 7);
            MakeNote(s, new Vector3(-5.8f, 1.5f, -1f), 90f, "note_scrawl", "Scratched into the paint",
                "IT DOES NOT SEE.\nIT LISTENS.\n\nWALK SOFT. HOLD YOUR BREATH.");
            // The trail: from under the barricaded quarters door, across the hall, up the corridor,
            // ending at the generator room door. Whatever was dragged went where you must go.
            StationPropKit.BloodPool(s, k, new Vector3(-5.5f, 0, 1f), 1.4f);
            StationPropKit.BloodTrail(s, k, new Vector3(-5f, 0, 1f), new Vector3(0f, 0, 3.5f), 5, seed: 13);
            StationPropKit.BloodTrail(s, k, new Vector3(0f, 0, 4.5f), new Vector3(-2f, 0, 9.4f), 5, seed: 17);

            // The quarters' hall door: boarded from the inside. The planks are intact.
            StationPropKit.Box(s, "SealedDoorSlab", new Vector3(-5.86f, 1.1f, 1f), new Vector3(0.12f, 2.2f, 1.6f), k.Door);
            StationPropKit.BarricadePlanks(s, k, new Vector3(-5.72f, 0, 1f), 1.6f, 90f, seed: 19);

            // KITCHEN — the kettle nobody dared to boil.
            StationPropKit.KitchenCounter(s, k, new Vector3(-11.5f, 0, -7.5f), 6f, 0f);
            StationPropKit.Stove(s, k, new Vector3(-15.3f, 0, -7.5f), 0f);
            StationPropKit.Table(s, k, new Vector3(-10.5f, 0, -4.5f), 20f, overturned: false);
            StationPropKit.FoodCans(s, k, new Vector3(-10.5f, 0.78f, -4.5f), seed: 23);
            StationPropKit.Chair(s, k, new Vector3(-11.7f, 0, -4f), 75f, tipped: false);
            StationPropKit.Chair(s, k, new Vector3(-9.4f, 0, -5.2f), 320f, tipped: true);

            // SLEEPING QUARTERS — the panic room. The barricade held. It didn't matter.
            StationPropKit.BunkBed(s, k, new Vector3(-13.5f, 0, -1.2f), 0f);
            StationPropKit.BunkBed(s, k, new Vector3(-13.5f, 0, 3.2f), 0f, bloodied: true);
            StationPropKit.Locker(s, k, new Vector3(-6.7f, 0, 2.4f), -90f, ajar: true);
            StationPropKit.Locker(s, k, new Vector3(-6.7f, 0, 3.2f), -90f, ajar: false);
            StationPropKit.FoodCans(s, k, new Vector3(-15.2f, 0.08f, 2.2f), seed: 29);
            StationPropKit.Box(s, "Lantern", new Vector3(-13.6f, 0.16f, 1.6f), new Vector3(0.22f, 0.32f, 0.22f), k.Rust, 0f, collider: false);
            StationPropKit.BloodPool(s, k, new Vector3(-6.6f, 0, 1f), 1.3f);
            StationPropKit.BarricadePlanks(s, k, new Vector3(-6.2f, 0, 1f), 1.6f, 90f, seed: 31);
            MakeNote(s, new Vector3(-13f, 0.62f, -1.2f), 0f, "note_letter", "Unsent letter",
                "Astrid — the ice sings at night now, and something on the air sings back. Nilsen swears we called " +
                "it down with the array, that it climbed out of our own signal. I don't care what it is.\n\n" +
                "I care that the kettle has stood cold on the stove for three weeks, because not one of us will " +
                "risk the whistle. When the relay is fixed I will send three words: COME GET US.");
            MakeNote(s, new Vector3(-12.6f, 1.3f, 3.05f), 0f, "note_panic", "Nailed above the bunk",
                "We boarded the hall door and slept in shifts.\n\nIt knocks now. It never knocked before. " +
                "It learned that from US.\n\nDo not answer knocking. Do not answer your own name.");

            // BATHROOM — pitch dark. A drip. A mirror your flashlight finds before you do.
            StationPropKit.Sink(s, k, new Vector3(-15.5f, 0, 6f), 90f);
            StationPropKit.Sink(s, k, new Vector3(-15.5f, 0, 7.2f), 90f);
            StationPropKit.MirrorStrip(s, k, new Vector3(-15.82f, 1.5f, 6.6f), 90f);
            StationPropKit.StallPartition(s, k, new Vector3(-13.8f, 0, 9f), 90f);
            StationPropKit.StallPartition(s, k, new Vector3(-12.6f, 0, 9f), 90f);
            StationPropKit.MopBucket(s, k, new Vector3(-12.2f, 0, 5f));
            StationPropKit.BloodPool(s, k, new Vector3(-14f, 0, 8.2f), 1.1f);

            // STORAGE — picked-over shelves; things worth throwing.
            StationPropKit.ShelfUnit(s, k, new Vector3(8.5f, 0, -7.3f), 0f);
            StationPropKit.ShelfUnit(s, k, new Vector3(15.3f, 0, -4f), 90f);
            StationPropKit.CrateStack(s, k, new Vector3(12.5f, 0, -7f));
            StationPropKit.Barrel(s, k, new Vector3(7.2f, 0, -3f));
            StationPropKit.Barrel(s, k, new Vector3(7.9f, 0, -3.7f), 30f);

            // FROZEN UTILITY — the outside is coming in through the window.
            StationPropKit.PipeRun(s, k, new Vector3(7, 2.2f, 5.72f), new Vector3(15.5f, 2.2f, 5.72f));
            StationPropKit.PipeRun(s, k, new Vector3(7, 1.6f, 5.8f), new Vector3(15.5f, 1.6f, 5.8f));
            StationPropKit.Valve(s, k, new Vector3(9, 1.6f, 5.8f));
            StationPropKit.Valve(s, k, new Vector3(13, 2.2f, 5.72f));
            StationPropKit.SnowDrift(s, k, new Vector3(14.6f, 0, 2f), 1.3f);
            StationPropKit.Shards(s, k, new Vector3(15f, 0, 2f), seed: 37);
            StationPropKit.SnowDrift(s, k, new Vector3(13.2f, 1.35f, 5.7f), 0.45f); // burst pipe, frozen mid-spray
            StationPropKit.Box(s, "FuseCrate", new Vector3(13.5f, 0.3f, 3.5f), new Vector3(0.7f, 0.6f, 0.7f), k.Crate, 15f);
            StationPropKit.Rubble(s, k, new Vector3(8.5f, 0, 5.4f), seed: 41); // collapsed passage north — no shortcut

            // MAINTENANCE CORRIDOR — pipes, tools, red light, and the end of the trail.
            StationPropKit.PipeRun(s, k, new Vector3(-10.8f, 2.1f, 4.3f), new Vector3(1.8f, 2.1f, 4.3f));
            StationPropKit.PipeRun(s, k, new Vector3(-10.8f, 1.9f, 9.72f), new Vector3(1.8f, 1.9f, 9.72f));
            StationPropKit.Valve(s, k, new Vector3(-6f, 1.9f, 9.72f));
            StationPropKit.ToolBox(s, k, new Vector3(-9.5f, 0, 8.8f), 25f);
            StationPropKit.WallHatch(s, k, new Vector3(-8f, 1.2f, 9.82f), 0f);
            StationPropKit.BloodPool(s, k, new Vector3(-2.2f, 0, 9.5f), 1.5f);

            // ELECTRICAL ROOM — dead panels waiting for one live fuse.
            StationPropKit.BreakerPanel(s, k, new Vector3(5.85f, 1.4f, 6.2f), -90f);
            StationPropKit.BreakerPanel(s, k, new Vector3(5.85f, 1.4f, 7.4f), -90f);
            StationPropKit.BreakerPanel(s, k, new Vector3(4f, 1.4f, 9.85f), 0f);
            StationPropKit.PipeRun(s, k, new Vector3(2.3f, 2.5f, 8.5f), new Vector3(5.8f, 2.5f, 8.5f), 0.1f);
            StationPropKit.Box(s, "HazardStrip", new Vector3(5.2f, 0.045f, 7f), new Vector3(1f, 0.01f, 3f), k.Hazard, 0f, collider: false);

            // GENERATOR ROOM & RADIO ROOM get dressed in BuildProgression (their props are the puzzle).
        }

        // ================================================================ ambience (the station's voice)

        private void BuildAmbience(Transform root)
        {
            var amb = new GameObject("Ambience").transform;
            amb.SetParent(root, false);

            // Global wind bed (2D — pure audio, never a stimulus; the Listener ignores weather).
            var windGo = new GameObject("Wind");
            windGo.transform.SetParent(amb, false);
            var wind = windGo.AddComponent<AudioSource>();
            ConfigureSource(wind, ProceduralAudio.WindLoop(), loop: true, volume: 0.32f, maxDistance: 500f, spatial: 0f);
            wind.Play();

            // The broken window howls locally.
            var howlGo = new GameObject("WindowHowl");
            howlGo.transform.SetParent(amb, false);
            howlGo.transform.position = new Vector3(15.8f, 1.8f, 2f);
            var howl = howlGo.AddComponent<AudioSource>();
            ConfigureSource(howl, ProceduralAudio.WindLoop(), loop: true, volume: 0.55f, maxDistance: 11f);
            howl.pitch = 1.18f;
            howl.Play();

            // Settling metal, cracking ice, a lonely drip, distant thumps — each also a real noise
            // stimulus, so the station's own voice occasionally tugs The Listener around the map.
            Periodic(amb, new Vector3(0, 2.8f, -2), 18f, 45f, 0.12f, 8f, new[] { ProceduralAudio.MetalCreak() });
            Periodic(amb, new Vector3(-11, 2.7f, 1), 25f, 60f, 0.10f, 7f, new[] { ProceduralAudio.MetalCreak() });
            Periodic(amb, new Vector3(12, 1.5f, 2), 12f, 30f, 0.18f, 8f, new[] { ProceduralAudio.IceCrack() });
            Periodic(amb, new Vector3(-15.5f, 1.0f, 6.6f), 4f, 11f, 0.05f, 3f, new[] { ProceduralAudio.DripPlink() });
            Periodic(amb, new Vector3(-2, 1.5f, 14), 25f, 55f, 0.20f, 12f, new[] { ProceduralAudio.DistantThump() });
        }

        private void Periodic(Transform p, Vector3 at, float min, float max, float loudness, float range, AudioClip[] clips)
        {
            var go = new GameObject("Ambient");
            go.transform.SetParent(p, false);
            go.transform.position = at;
            var src = go.AddComponent<AudioSource>();
            ConfigureSource(src, null, loop: false, volume: 0.7f, maxDistance: range + 6f);
            var em = go.AddComponent<PeriodicNoiseEmitter>();
            em.Configure(min, max, loudness, range, SoundPriority.Ambient, NoiseSourceKind.Generic);
            em.ConfigureAudio(src, clips);
        }

        // ================================================================ progression (key → fuse → generator → power → signal)

        private void BuildProgression(Transform root, ObjectiveTracker tracker, Dictionary<string, DoorRefs> doors, StationDirector director)
        {
            var g = new GameObject("Progression").transform;
            g.SetParent(root, false);

            // --- Items ---
            ItemDefinition keyDef = ItemDefinition.CreateRuntime("generator_key", "Generator Room Key",
                "A brass key on a leather fob, stamped 'GEN'. Cold as the man who kept it.", ItemCategory.Key);
            ItemDefinition fuseDef = ItemDefinition.CreateRuntime("fuse", "Ceramic Fuse",
                "A 30-amp ceramic fuse from the cold store. It still smells of ozone.", ItemCategory.Fuse);

            MakePickup(g, new Vector3(-6.95f, 0.95f, 2.4f), keyDef, new Vector3(0.24f, 0.1f, 0.34f));   // in the ajar locker
            MakePickup(g, new Vector3(13.5f, 0.66f, 3.5f), fuseDef, new Vector3(0.26f, 0.16f, 0.44f));  // on the crate under the window

            MakeBattery(g, new Vector3(-15.45f, 0.92f, 7.2f)); // bathroom sink — earn it in the dark
            MakeBattery(g, new Vector3(8.9f, 1.12f, -7.3f));   // storage shelf

            // --- Power grid ---
            var power = new GameObject("PowerMain").AddComponent<PowerController>();
            power.transform.SetParent(g, false);
            power.SetGridId("main");

            var radioLock = doors["radio"].Lockable.gameObject.AddComponent<PoweredLock>();
            radioLock.SetLock(doors["radio"].Lockable);
            radioLock.SetGridId("main");

            // --- Electrical room: the fuse box ---
            GameObject fuseGo = StationPropKit.Box(g, "FuseBox", new Vector3(5.62f, 1.3f, 8.4f), new Vector3(0.25f, 0.8f, 0.6f), _m.Panel);
            fuseGo.AddComponent<Highlighter>();
            var fuseBox = fuseGo.AddComponent<FuseBox>();
            fuseBox.SetRequiredFuse("fuse");
            fuseBox.SetInstallClip(ProceduralAudio.FuseClunk());
            var humSrc = fuseGo.AddComponent<AudioSource>();
            ConfigureSource(humSrc, ProceduralAudio.ElectricHumLoop(), loop: true, volume: 0.4f, maxDistance: 7f);
            fuseBox.Installed += humSrc.Play;

            // --- Generator room ---
            GameObject genGo = StationPropKit.GeneratorBody(g, _m, new Vector3(-2.5f, 0, 14.5f), 3.3f);
            genGo.AddComponent<Highlighter>();
            var loopSrc = genGo.AddComponent<AudioSource>();
            ConfigureSource(loopSrc, ProceduralAudio.GeneratorLoop(), loop: true, volume: 0.55f, maxDistance: 26f);
            var oneShotSrc = genGo.AddComponent<AudioSource>();
            ConfigureSource(oneShotSrc, null, loop: false, volume: 0.9f, maxDistance: 34f);
            var machine = genGo.AddComponent<MachineNoiseEmitter>();
            machine.ConfigureAudio(loopSrc, oneShotSrc, ProceduralAudio.GeneratorStart(), null);
            // Quiet steady hum so The Listener sweeps and leaves; the START is the loud beacon.
            machine.ConfigureHum(0.25f, 14f, SoundPriority.Minor);
            var generator = genGo.AddComponent<Generator>();
            generator.Configure(fuseBox, machine);
            generator.SetStartClip(null); // machine start spike already carries the audio
            StationPropKit.Barrel(g, _m, new Vector3(0.8f, 0, 16.5f), 10f);
            MakeNote(g, new Vector3(-3.5f, 1.25f, 13.55f), 0f, "note_starter", "Taped beside the starter",
                "FUSE FIRST — the panel is in the electrical room, back down the corridor.\n\n" +
                "Then the starter. She catches on the third crank, roaring like the end of the world. " +
                "You'll have a minute of echo to hide in. Do not be here when it goes quiet.");

            // --- Radio room ---
            Transform glow = StationPropKit.RadioConsole(g, _m, new Vector3(6f, 0, 16.6f), 180f);
            PointLight(glow, glow.position, new Color(0.28f, 0.55f, 0.50f), 0.5f, 3.5f, shadows: false);
            var staticSrc = glow.gameObject.AddComponent<AudioSource>();
            ConfigureSource(staticSrc, ProceduralAudio.RadioStaticLoop(), loop: true, volume: 0.3f, maxDistance: 9f);
            staticSrc.Play();
            StationPropKit.Chair(g, _m, new Vector3(6.6f, 0, 15.4f), 170f, tipped: false);
            StationPropKit.PaperScatter(g, _m, new Vector3(6.8f, 0, 14.6f), 4, seed: 43);
            StationPropKit.Rubble(g, _m, new Vector3(8.5f, 0, 10.8f), seed: 47);
            MakeNote(g, new Vector3(4.8f, 0.92f, 16.35f), 0f, "note_operator", "Operator's log — final entry",
                "03:11 — the pattern again under the static. Eight beats, a rest, eight beats. " +
                "Like something teaching us to answer.\n\n03:26 — Nilsen answered.\n\n" +
                "03:40 — the fence lights died south to north, one by one, quick as footsteps. " +
                "It came up the same path our signal went out.\n\nWhoever reads this: send the words, " +
                "then stay silent the rest of your life.");

            // --- Objective chain ---
            tracker.SetObjectives(new List<ObjectiveStep>
            {
                new ObjectiveStep("open_generator_room", "Get into the generator room.",
                    "The door at the end of the maintenance corridor is locked. Someone kept a key."),
                new ObjectiveStep("restore_power", "Restore power: fuse the electrical panel, then start the generator.",
                    "Spare fuses were kept in the cold store, past storage — where the window broke."),
                new ObjectiveStep("send_signal", "Send the distress signal from the radio room.",
                    "Power is up and the radio room has released. It heard the generator. Walk, don't run."),
            });

            doors["generator"].Lockable.Unlocked += () => tracker.CompleteObjective("open_generator_room");
            generator.Started += () =>
            {
                power.RestorePower();
                tracker.CompleteObjective("restore_power");
            };

            // --- The finish line: the console itself ---
            GameObject exitZone = new GameObject("SignalTrigger");
            exitZone.transform.SetParent(g, false);
            exitZone.transform.position = new Vector3(6f, 1.2f, 15.7f);
            var col = exitZone.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 2.4f, 2f);
            col.isTrigger = true;
            var exit = exitZone.AddComponent<SectionExit>();
            exit.Reached += () =>
            {
                tracker.CompleteObjective("send_signal");
                director.OnSectionComplete();
            };
        }

        private void MakePickup(Transform p, Vector3 at, ItemDefinition def, Vector3 size)
        {
            GameObject go = StationPropKit.Box(p, "Pickup_" + def.Id, at, size, _m.Rust);
            go.AddComponent<Highlighter>();
            go.AddComponent<ItemPickup>().SetItem(def, 1, ProceduralAudio.FuseClunk());
        }

        private void MakeBattery(Transform p, Vector3 at)
        {
            GameObject go = StationPropKit.Box(p, "Battery", at, new Vector3(0.16f, 0.2f, 0.16f), _m.Rust);
            go.AddComponent<Highlighter>();
            go.AddComponent<BatteryPickup>().Configure(55f, null, ProceduralAudio.FuseClunk());
        }

        private void MakeNote(Transform p, Vector3 at, float rotY, string id, string title, string body)
        {
            GameObject go = StationPropKit.Box(p, "Note_" + id, at, new Vector3(0.32f, 0.42f, 0.02f), _m.Paper, rotY);
            go.AddComponent<Highlighter>();
            go.AddComponent<ReadableNote>().SetNote(id, title, body);
        }

        // ================================================================ throwables (after the bake — they move)

        private void BuildThrowables(Transform root)
        {
            var t = new GameObject("Throwables").transform;
            t.SetParent(root, false);

            Vector3[] spots =
            {
                new Vector3(3.8f, 0.6f, -12.6f),   // entrance bench
                new Vector3(-9f, 1.1f, -7.5f),     // kitchen counter
                new Vector3(8.2f, 1.15f, -7.3f),   // storage shelf
                new Vector3(11f, 0.16f, -5f),      // storage floor
            };

            AudioClip[] clangs = ProceduralAudio.ImpactClangs();
            foreach (Vector3 at in spots)
            {
                GameObject can = StationPropKit.Can(t, _m, at);
                var body = can.AddComponent<Rigidbody>();
                body.mass = 0.5f;
                can.AddComponent<Highlighter>();
                can.AddComponent<Grabbable>();
                var src = can.AddComponent<AudioSource>();
                ConfigureSource(src, null, loop: false, volume: 0.85f, maxDistance: 18f);
                can.AddComponent<ImpactNoiseEmitter>().ConfigureAudio(src, clangs);
            }
        }

        // ================================================================ navmesh / enemy / player

        private void BakeNavMesh(Transform root)
        {
            for (int i = 0; i < _doorLeaves.Count; i++)
            {
                if (_doorLeaves[i] != null) _doorLeaves[i].SetActive(false);
            }

            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; // dressing without colliders never blocks paths
            surface.BuildNavMesh();

            for (int i = 0; i < _doorLeaves.Count; i++)
            {
                if (_doorLeaves[i] != null) _doorLeaves[i].SetActive(true);
            }
        }

        private ListenerController BuildEnemy(Transform root)
        {
            var enemyGo = new GameObject("TheListener");
            enemyGo.transform.SetParent(root, false);

            Vector3 start = new Vector3(-5f, 0f, 7f); // it keeps to the red corridor when calm
            if (NavMesh.SamplePosition(start, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                start = hit.position;
            }
            enemyGo.transform.position = start;

            StationPropKit.ListenerFigure(enemyGo.transform, _m);

            // Patrol: the corridor artery and a slow circuit of the hall.
            var routeGo = new GameObject("ListenerPatrol");
            routeGo.transform.SetParent(root, false);
            Vector3[] points =
            {
                new Vector3(-9f, 0, 7f), new Vector3(-2f, 0, 7f), new Vector3(0f, 0, 1f),
                new Vector3(3f, 0, -4f), new Vector3(-3f, 0, -2f)
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

            // Its voice: the rasp loop IS its visibility. Learn it. Track it. Fear it.
            var presence = enemyGo.AddComponent<AudioSource>();
            ConfigureSource(presence, ProceduralAudio.ListenerRaspLoop(), loop: true, volume: 0.25f, maxDistance: 16f);
            var stingSrc = enemyGo.AddComponent<AudioSource>();
            ConfigureSource(stingSrc, null, loop: false, volume: 0.9f, maxDistance: 30f);

            var audio = enemyGo.AddComponent<ListenerAudio>();
            audio.ConfigureAudio(presence, stingSrc,
                ProceduralAudio.StingAlerted(), ProceduralAudio.StingChase(),
                ProceduralAudio.StingAttack(), ProceduralAudio.StingLost());

            enemyGo.AddComponent<ListenerAnimator>();
            ListenerController controller = enemyGo.AddComponent<ListenerController>();
            controller.AssignPatrol(route);
            controller.Agent.radius = 0.35f;
            controller.Agent.height = 1.9f;
            controller.Agent.baseOffset = 0f;

            return controller;
        }

        private Transform SetupPlayer()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerGo == null)
            {
                Debug.LogWarning("[Vardo9] No 'Player'-tagged rig found. The GameBootstrap normally builds one.");
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

        // ================================================================ audio helper

        private static void ConfigureSource(AudioSource src, AudioClip clip, bool loop, float volume,
            float maxDistance, float spatial = 1f)
        {
            src.clip = clip;
            src.loop = loop;
            src.volume = volume;
            src.playOnAwake = false;
            src.spatialBlend = spatial;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = maxDistance;
        }
    }
}
