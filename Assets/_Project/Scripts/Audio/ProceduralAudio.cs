using UnityEngine;

namespace Nordo.Audio
{
    /// <summary>
    /// Nordo's sound design, synthesized at load: wind, generator rumble, electrical hum, radio
    /// static, the Listener's breathing, footsteps, door groans, metal clangs — all generated as
    /// deliberately lo-fi 22 kHz mono clips (the PSX-era grain is the aesthetic, not a compromise).
    /// <para>
    /// Everything is deterministic (fixed seeds), allocation happens once at level build, and no
    /// binary audio assets are needed — the whole soundscape lives in reviewable code and ships with
    /// the repo. Clips are cached so repeated calls return the same instance.
    /// </para>
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 22050; // lo-fi on purpose — PSX-era grain
        private const float Tau = Mathf.PI * 2f;

        private static readonly System.Collections.Generic.Dictionary<string, AudioClip> Cache = new();

        // ---------------------------------------------------------------- loops

        /// <summary>Arctic wind: dark filtered noise breathing on two slow, loop-perfect swells.</summary>
        public static AudioClip WindLoop()
        {
            return Cached("wind", () =>
            {
                float seconds = 10f;
                float[] d = NoiseBuffer(seconds, seed: 11);
                Lowpass(d, 0.035f);
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)d.Length; // 0..1 across the loop → periodic LFOs
                    float swell = 0.55f + 0.30f * Mathf.Sin(Tau * 3f * t) + 0.15f * Mathf.Sin(Tau * 7f * t + 1.3f);
                    d[i] *= swell;
                }
                d = MakeLoop(d, 0.5f);
                Normalize(d, 0.40f);
                return Bake("wind", d);
            });
        }

        /// <summary>Running diesel generator: detuned low partials with an 8 Hz mechanical wobble.</summary>
        public static AudioClip GeneratorLoop()
        {
            return Cached("generator", () =>
            {
                float seconds = 4f; // 55/110/164/8 Hz all divide 4 s → seamless
                float[] d = NoiseBuffer(seconds, seed: 23);
                Lowpass(d, 0.05f);
                float p1 = 0f, p2 = 0f, p3 = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    p1 += Tau * 55f / SampleRate; p2 += Tau * 110f / SampleRate; p3 += Tau * 164f / SampleRate;
                    float wobble = 1f + 0.22f * Mathf.Sin(Tau * 8f * t);
                    d[i] = d[i] * 0.18f + (Mathf.Sin(p1) * 0.5f + Mathf.Sin(p2) * 0.24f + Mathf.Sin(p3) * 0.11f) * wobble;
                }
                d = MakeLoop(d, 0.25f);
                Normalize(d, 0.5f);
                return Bake("generator", d);
            });
        }

        /// <summary>Live electrics: mains hum with harmonics. Quiet, constant, unnerving.</summary>
        public static AudioClip ElectricHumLoop()
        {
            return Cached("hum", () =>
            {
                float[] d = Buffer(2f); // 50/100/150 Hz divide 2 s → seamless
                float p1 = 0f, p2 = 0f, p3 = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    p1 += Tau * 50f / SampleRate; p2 += Tau * 100f / SampleRate; p3 += Tau * 150f / SampleRate;
                    d[i] = Mathf.Sin(p1) * 0.6f + Mathf.Sin(p2) * 0.28f + Mathf.Sin(p3) * 0.12f;
                }
                Normalize(d, 0.22f);
                return Bake("hum", d);
            });
        }

        /// <summary>Dead-channel radio static with irregular crackle spits.</summary>
        public static AudioClip RadioStaticLoop()
        {
            return Cached("static", () =>
            {
                float[] d = NoiseBuffer(3f, seed: 37);
                // High-pass by subtracting a lowpassed copy → thin, hissy.
                float[] lp = (float[])d.Clone();
                Lowpass(lp, 0.15f);
                var rng = new System.Random(38);
                for (int i = 0; i < d.Length; i++)
                {
                    d[i] = (d[i] - lp[i]) * 0.6f;
                    if (rng.NextDouble() < 0.0006) // crackle burst
                    {
                        int len = 20 + rng.Next(40);
                        float amp = 0.5f + (float)rng.NextDouble() * 0.5f;
                        for (int j = 0; j < len && i + j < d.Length; j++)
                        {
                            d[i + j] += ((float)rng.NextDouble() * 2f - 1f) * amp * (1f - j / (float)len);
                        }
                    }
                }
                d = MakeLoop(d, 0.2f);
                Normalize(d, 0.3f);
                return Bake("static", d);
            });
        }

        /// <summary>
        /// The Listener's presence: two slow, wet breath cycles over a sub-bass growl. This loop is the
        /// player's only reliable read on where the creature is — it IS the enemy's visibility.
        /// </summary>
        public static AudioClip ListenerRaspLoop()
        {
            return Cached("rasp", () =>
            {
                float seconds = 6f; // two 3 s breath cycles
                float[] d = NoiseBuffer(seconds, seed: 51);
                Lowpass(d, 0.06f);
                float growlPhase = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float cyc = Mathf.Sin(Tau * t / 3f);
                    // Asymmetric envelope: long scraping exhale, short sharp inhale.
                    float env = cyc > 0f ? Mathf.Pow(cyc, 1.3f) : 0.35f * Mathf.Pow(-cyc, 2.5f);
                    growlPhase += Tau * 44f / SampleRate;
                    d[i] = d[i] * env + Mathf.Sin(growlPhase) * 0.25f * env;
                }
                d = MakeLoop(d, 0.3f);
                Normalize(d, 0.45f);
                return Bake("rasp", d);
            });
        }

        // ---------------------------------------------------------------- one-shots: environment

        /// <summary>Long metal groan — the station settling in the cold.</summary>
        public static AudioClip MetalCreak() => Cached("creak", () => Groan("creak", 1.1f, 480f, 180f, seed: 61, peak: 0.32f));

        /// <summary>Sharp crack of stressed ice with glassy pings.</summary>
        public static AudioClip IceCrack()
        {
            return Cached("icecrack", () =>
            {
                float[] d = Buffer(0.5f);
                var rng = new System.Random(71);
                for (int i = 0; i < 240 && i < d.Length; i++)
                {
                    d[i] += ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-i / 40f);
                }
                AddPing(d, 2600f, 0.10f, 0.28f, 40f);
                AddPing(d, 3400f, 0.14f, 0.18f, 55f);
                Normalize(d, 0.38f);
                return Bake("icecrack", d);
            });
        }

        /// <summary>A distant, soft structural thump. Something moved. Somewhere.</summary>
        public static AudioClip DistantThump()
        {
            return Cached("thump", () =>
            {
                float[] d = NoiseBuffer(1f, seed: 83);
                Lowpass(d, 0.045f);
                float p = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    p += Tau * 58f / SampleRate;
                    d[i] = d[i] * 0.4f * Mathf.Exp(-t * 12f) + Mathf.Sin(p) * Mathf.Exp(-t * 8f) * 0.6f;
                }
                Normalize(d, 0.33f);
                return Bake("thump", d);
            });
        }

        // ---------------------------------------------------------------- one-shots: interaction

        /// <summary>Slow door groan for opening.</summary>
        public static AudioClip DoorOpen() => Cached("dooropen", () => Groan("dooropen", 1.3f, 240f, 95f, seed: 91, peak: 0.4f));

        /// <summary>Shorter groan ending in a frame thud for closing.</summary>
        public static AudioClip DoorClose()
        {
            return Cached("doorclose", () =>
            {
                float[] d = Buffer(0.65f);
                MixGroan(d, 0f, 0.3f, 300f, 160f, seed: 92, gain: 0.7f);
                AddThud(d, 0.34f, 85f, 0.8f);
                Normalize(d, 0.42f);
                return Bake("doorclose", d);
            });
        }

        /// <summary>Three quick metallic raps — a locked handle being tried.</summary>
        public static AudioClip LockedRattle()
        {
            return Cached("rattle", () =>
            {
                float[] d = Buffer(0.5f);
                var rng = new System.Random(97);
                for (int k = 0; k < 3; k++)
                {
                    int start = (int)(k * 0.13f * SampleRate);
                    for (int i = 0; i < 400 && start + i < d.Length; i++)
                    {
                        d[start + i] += ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-i / 70f) * 0.6f;
                    }
                    AddPing(d, 1240f + k * 60f, k * 0.13f, 0.35f, 45f);
                }
                Normalize(d, 0.42f);
                return Bake("rattle", d);
            });
        }

        /// <summary>Heavy bolt turning: thud + click.</summary>
        public static AudioClip UnlockClunk()
        {
            return Cached("unlock", () =>
            {
                float[] d = Buffer(0.4f);
                AddThud(d, 0.02f, 95f, 1f);
                AddPing(d, 900f, 0.16f, 0.4f, 60f);
                Normalize(d, 0.5f);
                return Bake("unlock", d);
            });
        }

        /// <summary>Ceramic fuse seating home with a charged snap.</summary>
        public static AudioClip FuseClunk()
        {
            return Cached("fuse", () =>
            {
                float[] d = Buffer(0.35f);
                AddThud(d, 0.02f, 130f, 0.8f);
                AddPing(d, 1800f, 0.12f, 0.3f, 70f);
                AddPing(d, 60f, 0.16f, 0.5f, 12f); // charge settling
                Normalize(d, 0.45f);
                return Bake("fuse", d);
            });
        }

        /// <summary>The generator catching: stuttering rumble climbing to full roar.</summary>
        public static AudioClip GeneratorStart()
        {
            return Cached("genstart", () =>
            {
                float[] d = NoiseBuffer(2.4f, seed: 101);
                Lowpass(d, 0.06f);
                float phase = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float f = Mathf.Lerp(18f, 55f, Mathf.Clamp01(t / 1.4f));
                    phase += Tau * f / SampleRate;
                    float ramp = Mathf.Clamp01(t / 0.9f);
                    float stutter = t < 0.9f ? 0.55f + 0.45f * Mathf.Sin(Tau * 13f * t) : 1f;
                    d[i] = (Mathf.Sin(phase) * 0.7f + d[i] * 0.45f * t) * ramp * stutter;
                }
                Normalize(d, 0.6f);
                return Bake("genstart", d);
            });
        }

        /// <summary>Set of hollow tin clangs for thrown objects (variants so repeats don't ring identical).</summary>
        public static AudioClip[] ImpactClangs(int count = 3)
        {
            var clips = new AudioClip[count];
            for (int v = 0; v < count; v++)
            {
                int variant = v;
                clips[v] = Cached($"clang{variant}", () =>
                {
                    var rng = new System.Random(113 + variant * 17);
                    float f0 = 520f + (float)rng.NextDouble() * 200f;
                    float[] d = Buffer(0.7f);
                    for (int i = 0; i < 140 && i < d.Length; i++)
                    {
                        d[i] += ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-i / 45f);
                    }
                    AddPing(d, f0, 0.005f, 0.5f, 9f);
                    AddPing(d, f0 * 1.83f, 0.005f, 0.3f, 12f);
                    AddPing(d, f0 * 2.71f, 0.005f, 0.18f, 16f);
                    Normalize(d, 0.5f);
                    return Bake($"clang{variant}", d);
                });
            }
            return clips;
        }

        // ---------------------------------------------------------------- one-shots: player

        /// <summary>Footstep variants. Concrete = dull scuffs; metal adds a plate ring.</summary>
        public static AudioClip[] FootstepSet(bool metal, int count = 4)
        {
            var clips = new AudioClip[count];
            for (int v = 0; v < count; v++)
            {
                int variant = v;
                bool m = metal;
                clips[v] = Cached($"step_{(m ? "metal" : "conc")}_{variant}", () =>
                {
                    var rng = new System.Random(131 + variant * 13 + (m ? 7 : 0));
                    float[] d = NoiseBuffer(0.11f, seed: 131 + variant * 13 + (m ? 7 : 0));
                    Lowpass(d, m ? 0.16f : 0.10f);
                    float decay = 42f + (float)rng.NextDouble() * 14f;
                    for (int i = 0; i < d.Length; i++)
                    {
                        d[i] *= Mathf.Exp(-i / (float)SampleRate * decay);
                    }
                    if (m)
                    {
                        AddPing(d, 720f + variant * 45f, 0.004f, 0.22f, 30f);
                    }
                    Normalize(d, 0.34f + (float)rng.NextDouble() * 0.08f);
                    return Bake($"step_{(m ? "metal" : "conc")}_{variant}", d);
                });
            }
            return clips;
        }

        /// <summary>Landing thud after a jump or drop.</summary>
        public static AudioClip LandThud()
        {
            return Cached("land", () =>
            {
                float[] d = NoiseBuffer(0.3f, seed: 151);
                Lowpass(d, 0.07f);
                float p = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    p += Tau * 55f / SampleRate;
                    d[i] = d[i] * Mathf.Exp(-t * 20f) + Mathf.Sin(p) * Mathf.Exp(-t * 14f) * 0.5f;
                }
                Normalize(d, 0.5f);
                return Bake("land", d);
            });
        }

        // ---------------------------------------------------------------- one-shots: The Listener

        /// <summary>Rising alert — it turned toward a sound.</summary>
        public static AudioClip StingAlerted()
        {
            return Cached("s_alert", () =>
            {
                float[] d = NoiseBuffer(0.8f, seed: 163);
                Lowpass(d, 0.2f);
                float p = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / 0.8f / SampleRate;
                    float f = Mathf.Lerp(620f, 940f, t);
                    p += Tau * f / SampleRate;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                    d[i] = (d[i] * 0.6f + Mathf.Sin(p) * 0.25f) * env;
                }
                Normalize(d, 0.38f);
                return Bake("s_alert", d);
            });
        }

        /// <summary>The hunt: beating dissonant tones under harsh breath-noise.</summary>
        public static AudioClip StingChase()
        {
            return Cached("s_chase", () =>
            {
                float[] d = NoiseBuffer(1.3f, seed: 167);
                Lowpass(d, 0.12f);
                float p1 = 0f, p2 = 0f, p3 = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / 1.3f / SampleRate;
                    p1 += Tau * 233f / SampleRate; p2 += Tau * 247f / SampleRate; p3 += Tau * 466f / SampleRate;
                    float env = Mathf.Clamp01(t * 6f) * Mathf.Clamp01((1f - t) * 2.5f);
                    d[i] = (d[i] * 0.5f + (Mathf.Sin(p1) + Mathf.Sin(p2)) * 0.22f + Mathf.Sin(p3) * 0.1f) * env;
                }
                Normalize(d, 0.5f);
                return Bake("s_chase", d);
            });
        }

        /// <summary>It lost you: a long falling tone, like disappointment.</summary>
        public static AudioClip StingLost()
        {
            return Cached("s_lost", () =>
            {
                float[] d = Buffer(1.1f);
                float p = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / 1.1f / SampleRate;
                    float f = Mathf.Lerp(420f, 150f, t);
                    p += Tau * f / SampleRate;
                    d[i] = Mathf.Sin(p) * Mathf.Exp(-t * 2.2f) * 0.6f;
                }
                Normalize(d, 0.32f);
                return Bake("s_lost", d);
            });
        }

        /// <summary>The strike.</summary>
        public static AudioClip StingAttack()
        {
            return Cached("s_attack", () =>
            {
                float[] d = NoiseBuffer(0.5f, seed: 173);
                float p = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / 0.5f / SampleRate;
                    float f = Mathf.Lerp(190f, 70f, t);
                    p += Tau * f / SampleRate;
                    d[i] = (d[i] * 0.8f + Mathf.Sin(p) * 0.7f) * Mathf.Exp(-t * 5f);
                }
                Normalize(d, 0.6f);
                return Bake("s_attack", d);
            });
        }

        /// <summary>Single water drip into a metal basin — the loneliest sound in the station.</summary>
        public static AudioClip DripPlink()
        {
            return Cached("drip", () =>
            {
                float[] d = Buffer(0.4f);
                AddPing(d, 1900f, 0.005f, 0.5f, 34f);
                AddPing(d, 2850f, 0.005f, 0.2f, 46f);
                AddThud(d, 0.02f, 240f, 0.15f);
                Normalize(d, 0.3f);
                return Bake("drip", d);
            });
        }

        // ---------------------------------------------------------------- DSP helpers

        private static AudioClip Cached(string key, System.Func<AudioClip> create)
        {
            if (Cache.TryGetValue(key, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            clip = create();
            Cache[key] = clip;
            return clip;
        }

        private static AudioClip Bake(string name, float[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
            }

            var clip = AudioClip.Create($"nordo_{name}", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float[] Buffer(float seconds) => new float[Mathf.Max(64, Mathf.RoundToInt(seconds * SampleRate))];

        private static float[] NoiseBuffer(float seconds, int seed)
        {
            float[] d = Buffer(seconds);
            var rng = new System.Random(seed);
            for (int i = 0; i < d.Length; i++)
            {
                d[i] = (float)rng.NextDouble() * 2f - 1f;
            }
            return d;
        }

        /// <summary>One-pole lowpass; small alpha = darker.</summary>
        private static void Lowpass(float[] d, float alpha)
        {
            float y = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                y += alpha * (d[i] - y);
                d[i] = y;
            }
        }

        private static void Normalize(float[] d, float peak)
        {
            float max = 1e-5f;
            for (int i = 0; i < d.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(d[i]));
            }

            float g = peak / max;
            for (int i = 0; i < d.Length; i++)
            {
                d[i] *= g;
            }
        }

        /// <summary>Overlap-adds the tail onto the head so the clip loops seamlessly, trimming the tail.</summary>
        private static float[] MakeLoop(float[] d, float fadeSeconds)
        {
            int fade = Mathf.Clamp(Mathf.RoundToInt(fadeSeconds * SampleRate), 32, d.Length / 3);
            var result = new float[d.Length - fade];
            System.Array.Copy(d, result, result.Length);
            for (int i = 0; i < fade; i++)
            {
                float t = i / (float)fade;
                result[i] = d[i] * t + d[result.Length + i] * (1f - t);
            }
            return result;
        }

        /// <summary>A decaying sine partial added at a start offset — the basis of pings and rings.</summary>
        private static void AddPing(float[] d, float freq, float startSeconds, float amp, float decay)
        {
            int start = Mathf.Clamp((int)(startSeconds * SampleRate), 0, d.Length - 1);
            float p = 0f;
            for (int i = start; i < d.Length; i++)
            {
                float t = (i - start) / (float)SampleRate;
                p += Tau * freq / SampleRate;
                d[i] += Mathf.Sin(p) * amp * Mathf.Exp(-t * decay);
            }
        }

        /// <summary>A soft low thud (short sine burst) at a start offset.</summary>
        private static void AddThud(float[] d, float startSeconds, float freq, float amp)
        {
            int start = Mathf.Clamp((int)(startSeconds * SampleRate), 0, d.Length - 1);
            float p = 0f;
            for (int i = start; i < d.Length; i++)
            {
                float t = (i - start) / (float)SampleRate;
                p += Tau * freq / SampleRate;
                d[i] += Mathf.Sin(p) * amp * Mathf.Exp(-t * 22f);
            }
        }

        /// <summary>A groaning frequency sweep with jitter — hinges and stressed metal.</summary>
        private static AudioClip Groan(string name, float seconds, float fFrom, float fTo, int seed, float peak)
        {
            float[] d = Buffer(seconds);
            MixGroan(d, 0f, seconds, fFrom, fTo, seed, 1f);
            Normalize(d, peak);
            return Bake(name, d);
        }

        private static void MixGroan(float[] d, float startSeconds, float durSeconds, float fFrom, float fTo, int seed, float gain)
        {
            var rng = new System.Random(seed);
            int start = Mathf.Clamp((int)(startSeconds * SampleRate), 0, d.Length - 1);
            int len = Mathf.Min((int)(durSeconds * SampleRate), d.Length - start);
            float p1 = 0f, p2 = 0f, jitter = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                jitter = Mathf.Clamp(jitter + ((float)rng.NextDouble() - 0.5f) * 6f, -25f, 25f);
                float f = Mathf.Lerp(fFrom, fTo, t) + jitter;
                p1 += Tau * f / SampleRate;
                p2 += Tau * f * 2.7f / SampleRate;
                float env = Mathf.Sin(Mathf.PI * t);
                float tremolo = 0.8f + 0.2f * Mathf.Sin(Tau * 6f * i / SampleRate);
                d[start + i] += (Mathf.Sin(p1) * 0.7f + Mathf.Sin(p2) * 0.2f) * env * tremolo * gain;
            }
        }
    }
}
