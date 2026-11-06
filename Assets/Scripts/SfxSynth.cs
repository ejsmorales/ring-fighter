using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Sound effects synthesized in code (placeholders to find the right feel; real recordings or AI/library sounds
    /// can replace them later). Every clip is made once and cached. Loops use whole numbers of cycles so they repeat
    /// without clicks.
    /// </summary>
    public static class SfxSynth
    {
        private const int Rate = 44100;

        private static AudioClip zap, launch, boom, laserLoop, chargeLoop, windLoop, shieldHum, block, reflect, rechargeLoop;

        // ---- One-shots ----

        /// <summary>Single bolt: a quick falling energy "pew".</summary>
        public static AudioClip Zap
        {
            get
            {
                if (zap != null) return zap;
                System.Random rng = new System.Random(1);
                zap = Make("Bolt Zap", 0.16f, (t, i) =>
                {
                    float f = 1500f * Mathf.Pow(260f / 1500f, t);
                    float env = Mathf.Min(1f, t * 60f) * Mathf.Pow(1f - t, 2f);
                    return env * (0.55f * Square(f, i) * 0.4f + 0.45f * Mathf.Sin(Phase(f, i)) + 0.15f * Noise(rng));
                }, true);
                return zap;
            }
        }

        /// <summary>Charged blast leaving the hands: a deep whoomp.</summary>
        public static AudioClip Launch
        {
            get
            {
                if (launch != null) return launch;
                System.Random rng = new System.Random(2);
                launch = Make("Blast Launch", 0.4f, (t, i) =>
                {
                    float f = 220f * Mathf.Pow(55f / 220f, t);
                    float env = Mathf.Min(1f, t * 40f) * Mathf.Pow(1f - t, 1.6f);
                    return env * (0.8f * Mathf.Sin(Phase(f, i)) + 0.25f * Noise(rng) * (1f - t));
                }, true);
                return launch;
            }
        }

        /// <summary>Charged blast exploding: rumbling noise with a sub-bass punch.</summary>
        public static AudioClip Boom
        {
            get
            {
                if (boom != null) return boom;
                System.Random rng = new System.Random(3);
                float low = 0f;
                boom = Make("Blast Boom", 1.1f, (t, i) =>
                {
                    low += (Noise(rng) - low) * Mathf.Lerp(0.25f, 0.03f, t);   // noise getting duller
                    float sub = Mathf.Sin(Phase(Mathf.Lerp(70f, 35f, t), i));
                    float env = Mathf.Min(1f, t * 200f) * Mathf.Pow(1f - t, 2.5f);
                    return env * (1.6f * low + 0.7f * sub);
                }, true);
                return boom;
            }
        }

        /// <summary>A shield stopping a bolt: bright metallic clang.</summary>
        public static AudioClip Block
        {
            get
            {
                if (block != null) return block;
                System.Random rng = new System.Random(4);
                block = Make("Shield Block", 0.35f, (t, i) =>
                {
                    float env = Mathf.Pow(1f - t, 3f);
                    float ring = Mathf.Sin(Phase(523f, i)) + 0.6f * Mathf.Sin(Phase(1317f, i)) + 0.35f * Mathf.Sin(Phase(2113f, i));
                    return env * 0.45f * ring + 0.4f * Noise(rng) * Mathf.Pow(1f - t, 30f);
                }, true);
                return block;
            }
        }

        /// <summary>A double shield reflecting a bolt: clang plus a rising zap.</summary>
        public static AudioClip Reflect
        {
            get
            {
                if (reflect != null) return reflect;
                System.Random rng = new System.Random(5);
                reflect = Make("Shield Reflect", 0.4f, (t, i) =>
                {
                    float f = 300f * Mathf.Pow(1800f / 300f, t);
                    float env = Mathf.Pow(1f - t, 2f);
                    float ring = Mathf.Sin(Phase(660f, i)) + 0.5f * Mathf.Sin(Phase(1650f, i));
                    return env * (0.35f * ring + 0.35f * Square(f, i) * 0.5f) + 0.3f * Noise(rng) * Mathf.Pow(1f - t, 25f);
                }, true);
                return reflect;
            }
        }

        // ---- Loops ----

        /// <summary>Laser beam: a buzzing, slightly wobbling hum.</summary>
        public static AudioClip LaserLoop
        {
            get
            {
                if (laserLoop != null) return laserLoop;
                laserLoop = Make("Laser Hum", 1f, (t, i) =>
                {
                    float buzz = 0.7f + 0.3f * Mathf.Sin(Phase(30f, i));
                    float tone = Mathf.Sin(Phase(140f, i)) + 0.5f * Mathf.Sin(Phase(280f, i)) + 0.3f * Mathf.Sin(Phase(420f, i)) + 0.15f * Square(840f, i);
                    return 0.35f * buzz * tone;
                }, false);
                return laserLoop;
            }
        }

        /// <summary>Charged blast building: a pulsing tone (its pitch is raised with the charge by the player).</summary>
        public static AudioClip ChargeLoop
        {
            get
            {
                if (chargeLoop != null) return chargeLoop;
                chargeLoop = Make("Blast Charge", 1f, (t, i) =>
                {
                    float trem = 0.6f + 0.4f * Mathf.Sin(Phase(8f, i));
                    return 0.4f * trem * (Mathf.Sin(Phase(220f, i)) + 0.6f * Mathf.Sin(Phase(330f, i)) + 0.3f * Mathf.Sin(Phase(440f, i)));
                }, false);
                return chargeLoop;
            }
        }

        /// <summary>Whirlwind: swirling wind (filtered noise rising and falling).</summary>
        public static AudioClip WindLoop
        {
            get
            {
                if (windLoop != null) return windLoop;
                System.Random rng = new System.Random(6);
                float low = 0f, band = 0f;
                windLoop = MakeLoopedNoise("Whirlwind", 2f, (t, i) =>
                {
                    float sweep = 0.5f + 0.5f * Mathf.Sin(Phase(1.5f, i));   // 3 whole cycles in 2 s
                    float k = Mathf.Lerp(0.02f, 0.12f, sweep);
                    low += (Noise(rng) - low) * k;
                    band += (low - band) * 0.05f;
                    return (low - band) * Mathf.Lerp(2.2f, 3.6f, sweep) + 0.15f * Mathf.Sin(Phase(55f, i));
                });
                return windLoop;
            }
        }

        /// <summary>Shield held up: a low, shimmering hum.</summary>
        public static AudioClip ShieldHum
        {
            get
            {
                if (shieldHum != null) return shieldHum;
                shieldHum = Make("Shield Hum", 1f, (t, i) =>
                {
                    float shimmer = 0.8f + 0.2f * Mathf.Sin(Phase(6f, i));
                    return 0.3f * shimmer * (Mathf.Sin(Phase(90f, i)) + 0.5f * Mathf.Sin(Phase(180f, i)) + 0.25f * Mathf.Sin(Phase(271f, i)));
                }, false);
                return shieldHum;
            }
        }

        /// <summary>Recharge stance: a soft, bright chord that swells.</summary>
        public static AudioClip RechargeLoop
        {
            get
            {
                if (rechargeLoop != null) return rechargeLoop;
                rechargeLoop = Make("Recharge", 2f, (t, i) =>
                {
                    float swell = 0.7f + 0.3f * Mathf.Sin(Phase(2f, i));
                    float chord = Mathf.Sin(Phase(440f, i)) + Mathf.Sin(Phase(554f, i)) * 0.8f + Mathf.Sin(Phase(659f, i)) * 0.7f;
                    float sparkle = 0.15f * Mathf.Sin(Phase(1760f, i)) * (0.5f + 0.5f * Mathf.Sin(Phase(12f, i)));
                    return 0.18f * swell * chord + sparkle;
                }, false);
                return rechargeLoop;
            }
        }

        // ---- Helpers ----

        private delegate float Sample(float t, int i);

        private static float Phase(float hz, int i)
        {
            return 2f * Mathf.PI * hz * i / Rate;
        }

        private static float Square(float hz, int i)
        {
            return Mathf.Sin(Phase(hz, i)) >= 0f ? 1f : -1f;
        }

        private static float Noise(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        // t = 0..1 through the clip, i = sample index. One-shots get a tiny fade-out; loops are left as made.
        private static AudioClip Make(string name, float seconds, Sample f, bool oneShot)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(Rate * seconds));
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = f(i / (float)n, i);
                if (oneShot && i > n - 200) v *= (n - i) / 200f;
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Noise-based loop: generated a bit longer, then the end is cross-faded into the start so it repeats smoothly.
        private static AudioClip MakeLoopedNoise(string name, float seconds, Sample f)
        {
            int n = Mathf.RoundToInt(Rate * seconds);
            int fade = Rate / 5;
            float[] raw = new float[n + fade];
            for (int i = 0; i < raw.Length; i++) raw[i] = f(i / (float)n, i);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = raw[i];
                if (i < fade)
                {
                    float w = i / (float)fade;
                    v = raw[i] * w + raw[n + i] * (1f - w);
                }
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
