using System;
using UnityEngine;

namespace RingFighter
{
    /// <summary>How much visual-effect detail to draw. Cycled from the pause menu (A button).</summary>
    public enum VfxQuality
    {
        High = 0,      // all glow layers + full particle counts
        Low = 1,       // fewer glow layers, about 40% of the particles
        Minimal = 2    // fewest glow layers, no particles
    }

    /// <summary>
    /// Player-facing effect settings, remembered on the headset (PlayerPrefs). Effects read these when they are
    /// built or played, so a change applies to the next shot / hit, not to effects already on screen.
    /// </summary>
    public static class VfxSettings
    {
        private const string QualityKey = "RF_VfxQuality";
        private const string BloomKey = "RF_VfxBloom";

        private static bool loaded;
        private static VfxQuality quality = VfxQuality.High;
        private static bool bloom;

        /// <summary>Raised after quality or bloom changes.</summary>
        public static event Action Changed;

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            quality = (VfxQuality)Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, 0), 0, 2);
            bloom = PlayerPrefs.GetInt(BloomKey, 0) == 1;
        }

        public static VfxQuality Quality
        {
            get { Load(); return quality; }
            set
            {
                Load();
                if (quality == value) return;
                quality = value;
                PlayerPrefs.SetInt(QualityKey, (int)quality);
                PlayerPrefs.Save();
                if (Changed != null) Changed();
            }
        }

        /// <summary>Real post-processing bloom (costly on Quest). Off by default; the effects fake their glow.</summary>
        public static bool Bloom
        {
            get { Load(); return bloom; }
            set
            {
                Load();
                if (bloom == value) return;
                bloom = value;
                PlayerPrefs.SetInt(BloomKey, bloom ? 1 : 0);
                PlayerPrefs.Save();
                if (Changed != null) Changed();
            }
        }

        /// <summary>Multiplier for particle counts (0 = no particles).</summary>
        public static float ParticleScale
        {
            get
            {
                switch (Quality)
                {
                    case VfxQuality.High: return 1f;
                    case VfxQuality.Low: return 0.4f;
                    default: return 0f;
                }
            }
        }

        /// <summary>True when the extra, wider glow shell should be drawn around bolts, beams and orbs.</summary>
        public static bool ExtraHalos { get { return Quality == VfxQuality.High; } }

        public static string QualityLabel
        {
            get
            {
                switch (Quality)
                {
                    case VfxQuality.High: return "High";
                    case VfxQuality.Low: return "Low";
                    default: return "Minimal (no particles)";
                }
            }
        }

        public static void CycleQuality()
        {
            Quality = (VfxQuality)(((int)Quality + 1) % 3);
        }

        public static void ToggleBloom()
        {
            Bloom = !Bloom;
        }
    }
}
