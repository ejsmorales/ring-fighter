using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>Shader property IDs used by the effect shaders.</summary>
    public static class VfxIds
    {
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        public static readonly int PowerId = Shader.PropertyToID("_Power");
        public static readonly int TailFadeId = Shader.PropertyToID("_TailFade");
        public static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        public static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        public static readonly int ShimmerSpeedId = Shader.PropertyToID("_ShimmerSpeed");
        public static readonly int ShimmerAmountId = Shader.PropertyToID("_ShimmerAmount");
        public static readonly int ShimmerScaleId = Shader.PropertyToID("_ShimmerScale");
        public static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        public static readonly int RingRadiusId = Shader.PropertyToID("_RingRadius");
        public static readonly int RingWidthId = Shader.PropertyToID("_RingWidth");
        public static readonly int FillId = Shader.PropertyToID("_Fill");
        public static readonly int ArmsId = Shader.PropertyToID("_Arms");
        public static readonly int TwistId = Shader.PropertyToID("_Twist");
        public static readonly int SpinId = Shader.PropertyToID("_Spin");

        public static readonly int StreaksId = Shader.PropertyToID("_Streaks");
        public static readonly int SpeedId = Shader.PropertyToID("_Speed");

        public static readonly int HexScaleId = Shader.PropertyToID("_HexScale");
        public static readonly int FlashId = Shader.PropertyToID("_Flash");
        public static readonly int RippleCenterId = Shader.PropertyToID("_RippleCenter");
        public static readonly int RippleAgeId = Shader.PropertyToID("_RippleAge");
    }

    /// <summary>
    /// Makes the effect materials by cloning the template materials held by VfxLibrary (which the scene references,
    /// so the build keeps the shaders). Glow materials that never change are cached and shared; materials that
    /// animate (fade, ripple) are created per owner with the New... methods and destroyed by that owner.
    /// If the library is missing or incomplete, Ready is false and the old placeholder visuals stay on screen.
    /// </summary>
    public static class VfxMaterials
    {
        public static readonly Color Green = new Color(0.12f, 1f, 0.45f);
        public static readonly Color DeepGreen = new Color(0.08f, 0.85f, 0.38f);
        public static readonly Color HotWhite = new Color(0.88f, 1f, 0.93f);
        public static readonly Color EnemyYellow = new Color(1f, 0.8f, 0.12f);
        public static readonly Color EnemyOrange = new Color(1f, 0.38f, 0.06f);

        private static VfxLibrary library;
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static void SetLibrary(VfxLibrary lib)
        {
            if (library != lib) cache.Clear();
            library = lib;
        }

        public static bool Ready
        {
            get { return library != null && library.IsComplete; }
        }

        // ---- Glow ----

        /// <summary>A shared, never-animated glow material. 'key' must be unique per look.</summary>
        public static Material Glow(string key, Color color, float intensity, float power, float tailFade = 0f,
                                    float pulseSpeed = 0f, float pulseAmount = 0f,
                                    float shimmerSpeed = 0f, float shimmerAmount = 0f, float shimmerScale = 10f)
        {
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = NewGlow(color, intensity, power, tailFade, pulseSpeed, pulseAmount, shimmerSpeed, shimmerAmount, shimmerScale);
            if (m != null)
            {
                m.name = "RFVfx_" + key;
                cache[key] = m;
            }
            return m;
        }

        /// <summary>A glow material the caller owns (and must Destroy).</summary>
        public static Material NewGlow(Color color, float intensity, float power, float tailFade = 0f,
                                       float pulseSpeed = 0f, float pulseAmount = 0f,
                                       float shimmerSpeed = 0f, float shimmerAmount = 0f, float shimmerScale = 10f)
        {
            if (!Ready) return null;
            Material m = new Material(library.glow);
            m.SetColor(VfxIds.ColorId, color);
            m.SetFloat(VfxIds.IntensityId, intensity);
            m.SetFloat(VfxIds.PowerId, power);
            m.SetFloat(VfxIds.TailFadeId, tailFade);
            m.SetFloat(VfxIds.PulseSpeedId, pulseSpeed);
            m.SetFloat(VfxIds.PulseAmountId, pulseAmount);
            m.SetFloat(VfxIds.ShimmerSpeedId, shimmerSpeed);
            m.SetFloat(VfxIds.ShimmerAmountId, shimmerAmount);
            m.SetFloat(VfxIds.ShimmerScaleId, shimmerScale);
            m.SetFloat(VfxIds.AlphaId, 1f);
            return m;
        }

        // ---- Particles ----

        /// <summary>The one material every particle system shares (the library's own particle material).</summary>
        public static Material SharedParticle
        {
            get { return Ready ? library.particle : null; }
        }

        // ---- Ring / disc ----

        /// <summary>A ring-disc material the caller owns (and must Destroy).</summary>
        public static Material NewRing(Color color, float intensity, float ringRadius, float ringWidth,
                                       float fill = 0f, float arms = 0f, float twist = 1.5f, float spin = 2f)
        {
            if (!Ready) return null;
            Material m = new Material(library.ringDisc);
            m.SetColor(VfxIds.ColorId, color);
            m.SetFloat(VfxIds.IntensityId, intensity);
            m.SetFloat(VfxIds.RingRadiusId, ringRadius);
            m.SetFloat(VfxIds.RingWidthId, ringWidth);
            m.SetFloat(VfxIds.FillId, fill);
            m.SetFloat(VfxIds.ArmsId, arms);
            m.SetFloat(VfxIds.TwistId, twist);
            m.SetFloat(VfxIds.SpinId, spin);
            m.SetFloat(VfxIds.AlphaId, 1f);
            return m;
        }

        // ---- Funnel ----

        /// <summary>A funnel material the caller owns (and must Destroy).</summary>
        public static Material NewFunnel(Color color, float intensity, float streaks, float twist, float speed)
        {
            if (!Ready) return null;
            Material m = new Material(library.funnel);
            m.SetColor(VfxIds.ColorId, color);
            m.SetFloat(VfxIds.IntensityId, intensity);
            m.SetFloat(VfxIds.StreaksId, streaks);
            m.SetFloat(VfxIds.TwistId, twist);
            m.SetFloat(VfxIds.SpeedId, speed);
            m.SetFloat(VfxIds.AlphaId, 1f);
            return m;
        }

        // ---- Shield ----

        /// <summary>A shield material the caller owns (and must Destroy).</summary>
        public static Material NewShield(Color color, float intensity)
        {
            if (!Ready) return null;
            Material m = new Material(library.shield);
            m.SetColor(VfxIds.ColorId, color);
            m.SetFloat(VfxIds.IntensityId, intensity);
            m.SetFloat(VfxIds.AlphaId, 1f);
            m.SetFloat(VfxIds.RippleAgeId, 99f);
            return m;
        }
    }
}
