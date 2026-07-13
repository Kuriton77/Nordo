using UnityEngine;

namespace Nordo.Rendering
{
    /// <summary>
    /// Nordo's texture art, generated at load: 128×128, point-filtered, mip-free surfaces baked in
    /// the locked cold palette — concrete, riveted steel, treadplate, frosted metal, wood, snow, ice,
    /// blood, paper, control panels. This is what gives the PSX shader's affine warp and vertex
    /// wobble something to bite on; flat colours read as greybox, these read as a place.
    /// <para>Deterministic (hash-noise, fixed seeds), cached, and GPU-only after upload.</para>
    /// </summary>
    public static class PSXTextureLib
    {
        private const int S = 128;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> Cache = new();

        // Locked palette anchors (docs/ART_DIRECTION.md §3).
        private static readonly Color Void = C(0x0A, 0x0C, 0x10);
        private static readonly Color DeepSteel = C(0x14, 0x1A, 0x22);
        private static readonly Color Slate = C(0x23, 0x2B, 0x34);
        private static readonly Color ColdGrey = C(0x3A, 0x44, 0x4D);
        private static readonly Color IceHi = C(0x6E, 0x7C, 0x86);
        private static readonly Color Teal = C(0x2C, 0x4A, 0x47);
        private static readonly Color Amber = C(0xC8, 0x79, 0x1E);

        // ---------------------------------------------------------------- surfaces

        /// <summary>Stained, speckled concrete — the station's floors.</summary>
        public static Texture2D Concrete() => Cached("concrete", (x, y) =>
        {
            float v = Fbm(x / 14f, y / 14f, 3, 5) * 0.7f + 0.25f;
            v -= 0.10f * Fbm(x / 44f, y / 9f, 9, 2);          // long grime stains
            if (Hash(x, y, 17) > 0.985f) v -= 0.28f;           // dark speckle
            return Color.Lerp(Slate * 0.85f, ColdGrey, v);
        });

        /// <summary>Riveted steel wall panels with seams and vertical streaking.</summary>
        public static Texture2D MetalWall() => Cached("metalwall", (x, y) =>
        {
            float v = 0.5f + Fbm(x / 10f, y / 26f, 23, 3) * 0.3f;
            if (x % 32 < 1 || y % 32 < 1) v -= 0.30f;          // panel seams
            int lx = x % 32, ly = y % 32;
            if ((lx == 4 || lx == 27) && (ly == 4 || ly == 27)) v += 0.35f; // rivets
            return Color.Lerp(DeepSteel, ColdGrey, v);
        });

        /// <summary>Diamond treadplate for maintenance floors.</summary>
        public static Texture2D TreadPlate() => Cached("tread", (x, y) =>
        {
            float v = 0.35f + Fbm(x / 12f, y / 12f, 31, 2) * 0.2f;
            if ((x + y) % 12 < 2 || (x - y + 256) % 12 < 2) v += 0.22f;
            return Color.Lerp(DeepSteel, IceHi * 0.8f, v);
        });

        /// <summary>Cold, desaturated wood grain.</summary>
        public static Texture2D Wood() => Cached("wood", (x, y) =>
        {
            float grain = Mathf.Sin(x * 0.9f + Fbm(x / 9f, y / 30f, 41, 3) * 5f) * 0.5f + 0.5f;
            float v = 0.35f + grain * 0.3f + Fbm(x / 20f, y / 20f, 43, 2) * 0.15f;
            return Color.Lerp(new Color(0.20f, 0.17f, 0.14f), new Color(0.38f, 0.33f, 0.27f), v);
        });

        /// <summary>Crate planks: wood with board gaps and an edge frame.</summary>
        public static Texture2D Planks() => Cached("planks", (x, y) =>
        {
            float grain = Mathf.Sin(x * 0.9f + Fbm(x / 9f, y / 30f, 41, 3) * 5f) * 0.5f + 0.5f;
            float v = 0.35f + grain * 0.28f;
            if (y % 21 < 1) v -= 0.3f;                          // board gaps
            if (x < 5 || x > S - 6 || y < 5 || y > S - 6) v += 0.14f; // frame
            return Color.Lerp(new Color(0.19f, 0.16f, 0.13f), new Color(0.40f, 0.35f, 0.28f), v);
        });

        /// <summary>Wind-packed snow.</summary>
        public static Texture2D Snow() => Cached("snow", (x, y) =>
        {
            float v = 0.72f + Fbm(x / 16f, y / 16f, 53, 3) * 0.24f;
            return Color.Lerp(new Color(0.62f, 0.68f, 0.78f), new Color(0.88f, 0.92f, 0.98f), v);
        });

        /// <summary>Streaked pale ice.</summary>
        public static Texture2D Ice() => Cached("ice", (x, y) =>
        {
            float v = 0.45f + 0.3f * Mathf.Sin(y * 0.45f + Fbm(x / 8f, y / 24f, 59, 3) * 6f) * 0.5f
                      + Fbm(x / 18f, y / 18f, 61, 2) * 0.2f;
            if (Hash(x, y, 63) > 0.992f) v += 0.3f;             // glints
            return Color.Lerp(new Color(0.33f, 0.42f, 0.52f), new Color(0.66f, 0.76f, 0.88f), Mathf.Clamp01(v));
        });

        /// <summary>Steel eaten by rust blooms — barrels and old drums.</summary>
        public static Texture2D Rust() => Cached("rust", (x, y) =>
        {
            float m = Fbm(x / 13f, y / 13f, 67, 4);
            float v = 0.45f + Fbm(x / 9f, y / 22f, 69, 2) * 0.2f;
            Color steel = Color.Lerp(DeepSteel, ColdGrey, v);
            Color rust = new Color(0.33f, 0.19f, 0.11f);
            return Color.Lerp(steel, rust, Mathf.SmoothStep(0.45f, 0.75f, m));
        });

        /// <summary>Ruled, grimy paper for notes and logs.</summary>
        public static Texture2D Paper() => Cached("paper", (x, y) =>
        {
            float v = 0.82f + Fbm(x / 22f, y / 22f, 71, 2) * 0.12f;
            if (y % 12 == 3) v -= 0.16f;                        // ruled lines
            float edge = Mathf.Min(Mathf.Min(x, S - x), Mathf.Min(y, S - y)) / 22f;
            v -= Mathf.Clamp01(1f - edge) * 0.22f * Fbm(x / 7f, y / 7f, 73, 2); // grimy edges
            return Color.Lerp(new Color(0.45f, 0.47f, 0.48f), new Color(0.85f, 0.87f, 0.86f), Mathf.Clamp01(v));
        });

        /// <summary>Dead control panel: seams, dark switch rows, a few live-looking indicator dots.</summary>
        public static Texture2D ControlPanel() => Cached("panel", (x, y) =>
        {
            float v = 0.3f + Fbm(x / 15f, y / 15f, 79, 2) * 0.15f;
            Color c = Color.Lerp(Void, DeepSteel * 1.4f, v);
            if (x % 16 < 1 || y % 16 < 1) c = Void;             // module seams
            int cx = x % 16, cy = y % 16;
            if (cx >= 6 && cx <= 8 && cy >= 6 && cy <= 8)       // indicator dot per module
            {
                float r = Hash(x / 16, y / 16, 83);
                c = r > 0.92f ? Amber : r > 0.6f ? Teal * 1.6f : DeepSteel * 1.8f;
            }
            return c;
        });

        /// <summary>Diagonal hazard striping (amber/void) for machine bases.</summary>
        public static Texture2D Hazard() => Cached("hazard", (x, y) =>
        {
            bool stripe = (x + y) % 32 < 16;
            float wear = Fbm(x / 10f, y / 10f, 89, 2);
            Color c = stripe ? Amber * 0.75f : Void;
            return Color.Lerp(c, Slate, Mathf.SmoothStep(0.55f, 0.8f, wear)); // worn through
        });

        /// <summary>Dried blood pool — dark centre, ragged edges fading to the floor tone.</summary>
        public static Texture2D Blood() => Cached("blood", (x, y) =>
        {
            float dx = (x - S / 2f) / (S / 2f), dy = (y - S / 2f) / (S / 2f);
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float edge = Fbm(x / 11f, y / 11f, 97, 3) * 0.45f;
            float mask = Mathf.Clamp01(1.2f - r * 1.35f - edge);
            Color dried = new Color(0.22f, 0.05f, 0.05f);
            return Color.Lerp(Slate * 0.9f, dried, Mathf.SmoothStep(0.1f, 0.5f, mask));
        });

        /// <summary>Heavy steel with frost creeping in from the edges — the sealed outer door.</summary>
        public static Texture2D FrostSeal() => Cached("frostseal", (x, y) =>
        {
            float v = 0.45f + Fbm(x / 12f, y / 20f, 101, 3) * 0.25f;
            Color steel = Color.Lerp(DeepSteel, ColdGrey, v);
            float edge = Mathf.Min(Mathf.Min(x, S - x), Mathf.Min(y, S - y));
            float frost = Mathf.Clamp01(1f - edge / (14f + Fbm(x / 6f, y / 6f, 103, 3) * 26f));
            return Color.Lerp(steel, new Color(0.8f, 0.86f, 0.94f), frost * 0.85f);
        });

        /// <summary>Scratched bright metal — mirrors, sink fittings.</summary>
        public static Texture2D Mirror() => Cached("mirror", (x, y) =>
        {
            float v = 0.55f + Fbm(x / 30f, y / 8f, 107, 2) * 0.25f;
            if (Hash(0, y, 109) > 0.9f) v -= 0.12f;             // horizontal scratches
            return Color.Lerp(ColdGrey, new Color(0.72f, 0.78f, 0.84f), v);
        });

        // ---------------------------------------------------------------- machinery

        private static Texture2D Cached(string key, System.Func<int, int, Color> pixel)
        {
            if (Cache.TryGetValue(key, out Texture2D t) && t != null)
            {
                return t;
            }

            t = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                name = $"nordo_{key}",
                filterMode = FilterMode.Point,   // the crunch is the aesthetic
                wrapMode = TextureWrapMode.Repeat
            };

            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Color c = pixel(x, y);
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }

            t.SetPixels32(px);
            t.Apply(false, true); // no mips, release CPU copy
            Cache[key] = t;
            return t;
        }

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        /// <summary>Deterministic integer hash → [0,1).</summary>
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = seed;
                h = h * 73856093 ^ x * 19349663 ^ y * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        /// <summary>Smoothed value noise (tiles poorly but reads fine at prop scale).</summary>
        private static float ValueNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal noise, few octaves — cheap texture body.</summary>
        private static float Fbm(float x, float y, int seed, int octaves)
        {
            float v = 0f, amp = 0.5f, f = 1f;
            for (int o = 0; o < octaves; o++)
            {
                v += amp * ValueNoise(x * f, y * f, seed + o * 7);
                f *= 2f;
                amp *= 0.5f;
            }
            return v;
        }
    }
}
