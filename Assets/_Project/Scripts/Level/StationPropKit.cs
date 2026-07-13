using UnityEngine;
using Nordo.Rendering;

namespace Nordo.Level
{
    /// <summary>
    /// The set-dressing kit for Vardø-9: furniture, machinery, pipes, blood, snow, barricades — every
    /// visual noun the station is dressed with, composed from chunky low-poly boxes so silhouettes
    /// stay strong in the fog (art rule 10). Pure visuals: the kit never attaches gameplay
    /// components; the level builder decides what is interactive.
    /// </summary>
    public static class StationPropKit
    {
        /// <summary>Every material the station is built from, created once per level build.</summary>
        public struct MaterialSet
        {
            public Material Floor, Wall, MetalWall, Tread, IceFloor, SnowMat, Wood, Crate, Rust,
                            Paper, Panel, Hazard, Blood, Door, SealedDoor, VoidBlack, Mirror;
        }

        /// <summary>Builds the full palette of PSX materials (pipeline-aware, texture-baked).</summary>
        public static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                Floor = Mat(PSXTextureLib.Concrete(), Color.white, 3.5f),
                Wall = Mat(PSXTextureLib.MetalWall(), Color.white, 2f),
                MetalWall = Mat(PSXTextureLib.MetalWall(), new Color(0.85f, 0.9f, 0.95f), 2f),
                Tread = Mat(PSXTextureLib.TreadPlate(), Color.white, 3f),
                IceFloor = Mat(PSXTextureLib.Ice(), Color.white, 2.5f),
                SnowMat = Mat(PSXTextureLib.Snow(), Color.white, 1.5f),
                Wood = Mat(PSXTextureLib.Wood(), Color.white, 1f),
                Crate = Mat(PSXTextureLib.Planks(), Color.white, 1f),
                Rust = Mat(PSXTextureLib.Rust(), Color.white, 1f),
                Paper = Mat(PSXTextureLib.Paper(), Color.white, 1f),
                Panel = Mat(PSXTextureLib.ControlPanel(), Color.white, 1f),
                Hazard = Mat(PSXTextureLib.Hazard(), Color.white, 1f),
                Blood = Mat(PSXTextureLib.Blood(), Color.white, 1f),
                Door = Mat(PSXTextureLib.MetalWall(), new Color(0.75f, 0.72f, 0.68f), 1f),
                SealedDoor = Mat(PSXTextureLib.FrostSeal(), Color.white, 1f),
                VoidBlack = Mat(null, new Color(0.03f, 0.035f, 0.05f), 1f),
                Mirror = Mat(PSXTextureLib.Mirror(), Color.white, 1f)
            };
        }

        // ---------------------------------------------------------------- core builders

        /// <summary>The fundamental unit: a textured box, optionally collider-free (pure dressing).</summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size,
            Material mat, float rotY = 0f, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider)
            {
                Object.Destroy(go.GetComponent<BoxCollider>());
            }
            return go;
        }

        /// <summary>A box with full Euler rotation, for tipped/fallen dressing.</summary>
        public static GameObject BoxR(Transform parent, string name, Vector3 center, Vector3 size,
            Material mat, Vector3 euler, bool collider = true)
        {
            GameObject go = Box(parent, name, center, size, mat, 0f, collider);
            go.transform.rotation = Quaternion.Euler(euler);
            return go;
        }

        // ---------------------------------------------------------------- furniture

        public static void Table(Transform p, MaterialSet m, Vector3 at, float rotY, bool overturned)
        {
            if (!overturned)
            {
                Box(p, "Table", at + new Vector3(0, 0.72f, 0), new Vector3(1.6f, 0.06f, 0.9f), m.Wood, rotY);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 off = Quaternion.Euler(0, rotY, 0) *
                        new Vector3(i % 2 == 0 ? -0.7f : 0.7f, 0.36f, i < 2 ? -0.36f : 0.36f);
                    Box(p, "TableLeg", at + off, new Vector3(0.08f, 0.72f, 0.08f), m.Wood, rotY);
                }
            }
            else
            {
                // Flipped onto its side — someone went over or through it.
                BoxR(p, "Table_Overturned", at + new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.06f, 0.9f), m.Wood,
                    new Vector3(0f, rotY, 96f));
                BoxR(p, "TableLegs", at + new Vector3(0.45f, 0.5f, 0), new Vector3(0.08f, 0.72f, 0.7f), m.Wood,
                    new Vector3(0f, rotY, 96f), collider: false);
            }
        }

        public static void Chair(Transform p, MaterialSet m, Vector3 at, float rotY, bool tipped)
        {
            if (!tipped)
            {
                Box(p, "ChairSeat", at + new Vector3(0, 0.45f, 0), new Vector3(0.42f, 0.05f, 0.42f), m.Wood, rotY);
                Vector3 backOff = Quaternion.Euler(0, rotY, 0) * new Vector3(0, 0.75f, -0.19f);
                Box(p, "ChairBack", at + backOff, new Vector3(0.42f, 0.55f, 0.05f), m.Wood, rotY);
                Box(p, "ChairLegs", at + new Vector3(0, 0.21f, 0), new Vector3(0.36f, 0.42f, 0.36f), m.VoidBlack, rotY, collider: false);
            }
            else
            {
                BoxR(p, "Chair_Tipped", at + new Vector3(0, 0.22f, 0), new Vector3(0.42f, 0.05f, 0.42f), m.Wood,
                    new Vector3(84f, rotY, 0f));
                BoxR(p, "ChairBack_Tipped", at + new Vector3(0, 0.28f, 0.35f), new Vector3(0.42f, 0.55f, 0.05f), m.Wood,
                    new Vector3(84f, rotY, 0f), collider: false);
            }
        }

        public static void Bench(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "Bench", at + new Vector3(0, 0.42f, 0), new Vector3(1.7f, 0.07f, 0.4f), m.Wood, rotY);
            Box(p, "BenchLegs", at + new Vector3(0, 0.2f, 0), new Vector3(1.5f, 0.4f, 0.3f), m.VoidBlack, rotY, collider: false);
        }

        public static void BunkBed(Transform p, MaterialSet m, Vector3 at, float rotY, bool bloodied = false)
        {
            Box(p, "BunkFrame", at + new Vector3(0, 0.9f, 0), new Vector3(2f, 1.8f, 0.06f), m.MetalWall, rotY, collider: false);
            Box(p, "BunkLow", at + new Vector3(0, 0.45f, 0.45f), new Vector3(2f, 0.12f, 0.9f), m.Wall, rotY);
            Box(p, "BunkHigh", at + new Vector3(0, 1.25f, 0.45f), new Vector3(2f, 0.12f, 0.9f), m.Wall, rotY);
            Box(p, "BunkBlanket", at + new Vector3(0.2f, 0.53f, 0.45f), new Vector3(1.2f, 0.06f, 0.8f),
                bloodied ? m.Blood : m.Wood, rotY, collider: false);
        }

        public static void Locker(Transform p, MaterialSet m, Vector3 at, float rotY, bool ajar)
        {
            Box(p, "Locker", at + new Vector3(0, 0.95f, 0), new Vector3(0.6f, 1.9f, 0.5f), m.MetalWall, rotY);
            if (ajar)
            {
                Vector3 doorOff = Quaternion.Euler(0, rotY, 0) * new Vector3(0.28f, 0.95f, 0.3f);
                Box(p, "LockerDoor", at + doorOff, new Vector3(0.55f, 1.8f, 0.04f), m.MetalWall, rotY + 35f, collider: false);
            }
        }

        public static void ShelfUnit(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "ShelfFrameL", at + new Vector3(-0.85f, 0.9f, 0), new Vector3(0.08f, 1.8f, 0.5f), m.MetalWall, rotY);
            Box(p, "ShelfFrameR", at + new Vector3(0.85f, 0.9f, 0), new Vector3(0.08f, 1.8f, 0.5f), m.MetalWall, rotY);
            for (int i = 0; i < 3; i++)
            {
                Box(p, "ShelfBoard", at + new Vector3(0, 0.45f + i * 0.6f, 0), new Vector3(1.8f, 0.05f, 0.5f), m.Wood, rotY);
            }
            // A few stored boxes, one knocked askew.
            Box(p, "StoredBox", at + new Vector3(-0.4f, 0.62f, 0), new Vector3(0.4f, 0.3f, 0.4f), m.Crate, rotY + 4f, collider: false);
            Box(p, "StoredBox", at + new Vector3(0.45f, 1.22f, 0.05f), new Vector3(0.35f, 0.28f, 0.35f), m.Crate, rotY - 11f, collider: false);
        }

        public static void CrateStack(Transform p, MaterialSet m, Vector3 at)
        {
            Box(p, "Crate", at + new Vector3(0, 0.3f, 0), new Vector3(0.7f, 0.6f, 0.7f), m.Crate, 5f);
            Box(p, "Crate", at + new Vector3(0.75f, 0.28f, 0.1f), new Vector3(0.6f, 0.56f, 0.6f), m.Crate, -12f);
            Box(p, "Crate", at + new Vector3(0.3f, 0.85f, 0.05f), new Vector3(0.55f, 0.5f, 0.55f), m.Crate, 24f);
        }

        public static void Barrel(Transform p, MaterialSet m, Vector3 at, float rotY = 0f)
        {
            Box(p, "Barrel", at + new Vector3(0, 0.45f, 0), new Vector3(0.55f, 0.9f, 0.55f), m.Rust, rotY);
            Box(p, "BarrelLid", at + new Vector3(0, 0.92f, 0), new Vector3(0.5f, 0.05f, 0.5f), m.VoidBlack, rotY, collider: false);
        }

        // ---------------------------------------------------------------- kitchen / bathroom

        public static void KitchenCounter(Transform p, MaterialSet m, Vector3 at, float length, float rotY)
        {
            Box(p, "CounterBody", at + new Vector3(0, 0.45f, 0), new Vector3(length, 0.9f, 0.65f), m.MetalWall, rotY);
            Box(p, "CounterTop", at + new Vector3(0, 0.93f, 0), new Vector3(length + 0.06f, 0.06f, 0.7f), m.Mirror, rotY);
        }

        public static void Stove(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "Stove", at + new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.9f, 0.65f), m.VoidBlack, rotY);
            Box(p, "StoveTop", at + new Vector3(0, 0.92f, 0), new Vector3(0.66f, 0.04f, 0.6f), m.MetalWall, rotY, collider: false);
            // The kettle nobody dared to boil (note 3 references it).
            Box(p, "Kettle", at + new Vector3(0.12f, 1.05f, 0.05f), new Vector3(0.22f, 0.22f, 0.22f), m.Rust, rotY, collider: false);
        }

        public static void Sink(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "SinkPedestal", at + new Vector3(0, 0.35f, 0), new Vector3(0.3f, 0.7f, 0.3f), m.Wall, rotY);
            Box(p, "SinkBasin", at + new Vector3(0, 0.78f, 0), new Vector3(0.55f, 0.18f, 0.45f), m.Mirror, rotY);
        }

        public static void MirrorStrip(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "Mirror", at, new Vector3(1.4f, 0.7f, 0.03f), m.Mirror, rotY, collider: false);
        }

        public static void StallPartition(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "Stall", at + new Vector3(0, 1.0f, 0), new Vector3(1.1f, 1.7f, 0.05f), m.Wall, rotY);
        }

        public static void MopBucket(Transform p, MaterialSet m, Vector3 at)
        {
            BoxR(p, "Bucket_Tipped", at + new Vector3(0, 0.16f, 0), new Vector3(0.32f, 0.34f, 0.32f), m.Rust,
                new Vector3(0f, 30f, 100f), collider: false);
            Box(p, "SpiltWater", at + new Vector3(0.35f, 0.012f, 0.1f), new Vector3(0.8f, 0.02f, 0.6f), m.IceFloor, 15f, collider: false);
        }

        // ---------------------------------------------------------------- industrial

        public static void PipeRun(Transform p, MaterialSet m, Vector3 from, Vector3 to, float thickness = 0.14f)
        {
            Vector3 mid = (from + to) * 0.5f;
            Vector3 d = to - from;
            var go = Box(p, "Pipe", mid, new Vector3(Mathf.Max(Mathf.Abs(d.x), thickness),
                Mathf.Max(Mathf.Abs(d.y), thickness), Mathf.Max(Mathf.Abs(d.z), thickness)), m.MetalWall, 0f, collider: false);
            go.name = "Pipe";
        }

        public static void Valve(Transform p, MaterialSet m, Vector3 at)
        {
            Box(p, "ValveBody", at, new Vector3(0.2f, 0.2f, 0.2f), m.Rust, 0f, collider: false);
            BoxR(p, "ValveWheel", at + new Vector3(0, 0, -0.12f), new Vector3(0.3f, 0.3f, 0.05f), m.Rust,
                new Vector3(0f, 0f, 45f), collider: false);
        }

        public static void ToolBox(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "ToolBox", at + new Vector3(0, 0.14f, 0), new Vector3(0.5f, 0.28f, 0.28f), m.Rust, rotY);
            Box(p, "Wrench", at + new Vector3(0.4f, 0.03f, 0.1f), new Vector3(0.35f, 0.04f, 0.07f), m.MetalWall, rotY + 40f, collider: false);
        }

        public static void BreakerPanel(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "BreakerPanel", at, new Vector3(0.7f, 1.0f, 0.12f), m.Panel, rotY);
        }

        public static void WallHatch(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "Hatch", at, new Vector3(0.8f, 0.8f, 0.08f), m.MetalWall, rotY, collider: false);
            Box(p, "HatchHandle", at + Quaternion.Euler(0, rotY, 0) * new Vector3(0.25f, 0f, -0.06f),
                new Vector3(0.16f, 0.05f, 0.05f), m.Rust, rotY, collider: false);
        }

        /// <summary>The generator: engine block, housing, exhaust stack, hazard apron.</summary>
        public static GameObject GeneratorBody(Transform p, MaterialSet m, Vector3 at, float ceilingY)
        {
            var root = new GameObject("Generator");
            root.transform.SetParent(p, false);
            root.transform.position = at;

            Box(root.transform, "HazardApron", at + new Vector3(0, 0.015f, 0), new Vector3(2.6f, 0.03f, 2.2f), m.Hazard, 0f, collider: false);
            Box(root.transform, "EngineBlock", at + new Vector3(0, 0.75f, 0), new Vector3(1.7f, 1.5f, 1.2f), m.Rust);
            Box(root.transform, "Housing", at + new Vector3(0, 1.62f, 0), new Vector3(1.3f, 0.35f, 1f), m.MetalWall);
            Box(root.transform, "FuelTank", at + new Vector3(1.15f, 0.4f, 0.2f), new Vector3(0.5f, 0.8f, 0.5f), m.Rust, 8f);
            PipeRun(root.transform, m, at + new Vector3(-0.4f, 1.8f, 0), new Vector3(at.x - 0.4f, ceilingY, at.z), 0.18f);
            Box(root.transform, "StarterBox", at + new Vector3(-0.9f, 1.0f, 0.62f), new Vector3(0.35f, 0.45f, 0.12f), m.Panel);
            return root;
        }

        /// <summary>The radio console: desk, panel bank, dead dials. Returns the glow anchor.</summary>
        public static Transform RadioConsole(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            var root = new GameObject("RadioConsole");
            root.transform.SetParent(p, false);
            root.transform.position = at;
            Quaternion r = Quaternion.Euler(0, rotY, 0);

            Box(root.transform, "ConsoleDesk", at + r * new Vector3(0, 0.42f, 0), new Vector3(2.6f, 0.84f, 0.8f), m.MetalWall, rotY);
            Box(root.transform, "ConsoleBank", at + r * new Vector3(0, 1.25f, -0.28f), new Vector3(2.6f, 0.8f, 0.25f), m.Panel, rotY);
            Box(root.transform, "ConsoleSide", at + r * new Vector3(1.5f, 0.9f, -0.1f), new Vector3(0.4f, 1.8f, 0.6f), m.Panel, rotY);
            Box(root.transform, "Microphone", at + r * new Vector3(-0.5f, 0.95f, 0.1f), new Vector3(0.08f, 0.22f, 0.08f), m.VoidBlack, rotY, collider: false);

            var glow = new GameObject("ConsoleGlow").transform;
            glow.SetParent(root.transform, false);
            glow.position = at + r * new Vector3(0, 1.3f, 0.15f);
            return glow;
        }

        // ---------------------------------------------------------------- storytelling

        public static void BloodPool(Transform p, MaterialSet m, Vector3 at, float scale, float rotY = 0f)
        {
            Box(p, "BloodPool", at + new Vector3(0, 0.012f, 0), new Vector3(scale, 0.02f, scale * 0.8f), m.Blood, rotY, collider: false);
        }

        /// <summary>A drag trail: diminishing smears from a start point toward an end point.</summary>
        public static void BloodTrail(Transform p, MaterialSet m, Vector3 from, Vector3 to, int steps, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                Vector3 at = Vector3.Lerp(from, to, t);
                at.x += (float)(rng.NextDouble() - 0.5) * 0.5f;
                at.z += (float)(rng.NextDouble() - 0.5) * 0.5f;
                float s = Mathf.Lerp(1.0f, 0.35f, t) * (0.7f + (float)rng.NextDouble() * 0.5f);
                BloodPool(p, m, at, s, (float)rng.NextDouble() * 360f);
            }
        }

        /// <summary>Planks hammered across an opening — they held. It didn't matter.</summary>
        public static void BarricadePlanks(Transform p, MaterialSet m, Vector3 center, float width, float wallRotY, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.55f + i * 0.55f + (float)rng.NextDouble() * 0.12f;
                float tilt = -14f + (float)rng.NextDouble() * 28f;
                BoxR(p, "BarricadePlank", center + new Vector3(0, y, 0),
                    new Vector3(width + 0.5f, 0.16f, 0.05f), m.Wood,
                    new Vector3(0f, wallRotY, tilt), collider: false);
            }
        }

        public static void SnowDrift(Transform p, MaterialSet m, Vector3 at, float scale)
        {
            Box(p, "SnowDrift", at + new Vector3(0, 0.12f * scale, 0), new Vector3(1.8f, 0.3f, 1.4f) * scale, m.SnowMat, 20f, collider: false);
            Box(p, "SnowDrift", at + new Vector3(0.3f * scale, 0.3f * scale, 0.1f), new Vector3(1.1f, 0.28f, 0.9f) * scale, m.SnowMat, -15f, collider: false);
            Box(p, "SnowDrift", at + new Vector3(0.1f, 0.45f * scale, -0.15f * scale), new Vector3(0.6f, 0.22f, 0.5f) * scale, m.SnowMat, 40f, collider: false);
        }

        /// <summary>Glass/ice shards around a broken window.</summary>
        public static void Shards(Transform p, MaterialSet m, Vector3 at, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < 7; i++)
            {
                Vector3 off = new Vector3((float)(rng.NextDouble() - 0.5) * 2f, 0.02f, (float)(rng.NextDouble() - 0.5) * 1.6f);
                BoxR(p, "Shard", at + off, new Vector3(0.18f, 0.015f, 0.12f), m.IceFloor,
                    new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), collider: false);
            }
        }

        public static void PaperScatter(Transform p, MaterialSet m, Vector3 at, int count, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                Vector3 off = new Vector3((float)(rng.NextDouble() - 0.5) * 2.4f, 0.012f, (float)(rng.NextDouble() - 0.5) * 2.4f);
                Box(p, "Paper", at + off, new Vector3(0.24f, 0.005f, 0.32f), m.Paper,
                    (float)rng.NextDouble() * 360f, collider: false);
            }
        }

        public static void CoatRack(Transform p, MaterialSet m, Vector3 at, float rotY)
        {
            Box(p, "CoatRail", at + new Vector3(0, 1.7f, 0), new Vector3(1.6f, 0.06f, 0.06f), m.MetalWall, rotY, collider: false);
            Box(p, "HangingCoat", at + new Vector3(-0.35f, 1.25f, 0), new Vector3(0.45f, 0.85f, 0.18f), m.VoidBlack, rotY, collider: false);
            Box(p, "HangingCoat", at + new Vector3(0.4f, 1.28f, 0), new Vector3(0.4f, 0.8f, 0.16f), m.Wood, rotY + 6f, collider: false);
        }

        public static void FrostPatch(Transform p, MaterialSet m, Vector3 at, float rotY, float scale)
        {
            Box(p, "Frost", at, new Vector3(1.2f * scale, 0.9f * scale, 0.02f), m.SnowMat, rotY, collider: false);
        }

        public static void Rubble(Transform p, MaterialSet m, Vector3 at, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                Vector3 off = new Vector3((float)(rng.NextDouble() - 0.5) * 1.6f,
                    0.15f + (float)rng.NextDouble() * 0.3f, (float)(rng.NextDouble() - 0.5) * 1.2f);
                BoxR(p, "Rubble", at + off,
                    new Vector3(0.3f + (float)rng.NextDouble() * 0.5f, 0.25f + (float)rng.NextDouble() * 0.3f, 0.3f + (float)rng.NextDouble() * 0.4f),
                    m.Wall, new Vector3((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f));
            }
        }

        /// <summary>A small tin can / enamel mug — visual body for a throwable (builder adds physics).</summary>
        public static GameObject Can(Transform p, MaterialSet m, Vector3 at)
        {
            return Box(p, "TinCan", at, new Vector3(0.16f, 0.24f, 0.16f), m.Rust, Random.Range(0f, 360f));
        }

        /// <summary>Cluster of frozen ration cans — meals that never happened.</summary>
        public static void FoodCans(Transform p, MaterialSet m, Vector3 at, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < 5; i++)
            {
                Vector3 off = new Vector3((float)(rng.NextDouble() - 0.5) * 0.7f, 0.08f, (float)(rng.NextDouble() - 0.5) * 0.5f);
                Box(p, "RationCan", at + off, new Vector3(0.14f, 0.16f, 0.14f), m.Rust,
                    (float)rng.NextDouble() * 360f, collider: false);
            }
        }

        /// <summary>
        /// The Listener's body: a gaunt, hunched black figure with too-long arms — pure silhouette,
        /// no face, no detail. It should only ever be read as a shape against the fog. Renderers only;
        /// the NavMeshAgent owns movement and there is no physics collider.
        /// </summary>
        public static void ListenerFigure(Transform root, MaterialSet m)
        {
            BoxR(root, "Torso", root.position + new Vector3(0, 1.2f, 0), new Vector3(0.5f, 1.0f, 0.32f), m.VoidBlack,
                new Vector3(14f, 0f, 0f), collider: false);
            BoxR(root, "Head", root.position + new Vector3(0, 1.82f, 0.14f), new Vector3(0.26f, 0.3f, 0.28f), m.VoidBlack,
                new Vector3(22f, 0f, 0f), collider: false);
            BoxR(root, "ArmL", root.position + new Vector3(-0.34f, 1.05f, 0.05f), new Vector3(0.11f, 1.05f, 0.11f), m.VoidBlack,
                new Vector3(8f, 0f, -7f), collider: false);
            BoxR(root, "ArmR", root.position + new Vector3(0.34f, 1.0f, 0.02f), new Vector3(0.11f, 1.15f, 0.11f), m.VoidBlack,
                new Vector3(6f, 0f, 9f), collider: false);
            Box(root, "LegL", root.position + new Vector3(-0.14f, 0.45f, 0), new Vector3(0.14f, 0.9f, 0.14f), m.VoidBlack, 0f, collider: false);
            Box(root, "LegR", root.position + new Vector3(0.14f, 0.44f, -0.03f), new Vector3(0.14f, 0.88f, 0.14f), m.VoidBlack, 0f, collider: false);
        }

        // ---------------------------------------------------------------- materials

        /// <summary>Pipeline-aware material: Nordo/PSX under URP, Standard otherwise. Never magenta.</summary>
        public static Material Mat(Texture2D texture, Color tint, float tiling)
        {
            bool urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = urp ? Shader.Find("Nordo/PSX") : null;
            if (shader == null && urp)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            if (texture != null)
            {
                mat.mainTexture = texture;                     // maps to _BaseMap ([MainTexture]) or _MainTex
                mat.mainTextureScale = new Vector2(tiling, tiling);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", tint);
            }
            mat.color = tint;
            return mat;
        }
    }
}
