using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Holds the effect shaders' template materials (Assets/VFX/Materials). It lives on the "VFX System" object that
    /// RingFighterSceneBuilder puts in the scene, so every effect shader is referenced by a material the scene
    /// uses. That is what keeps the Quest build from stripping the shaders. At runtime VfxMaterials clones these
    /// templates; nothing is loaded with Shader.Find or Resources.
    /// </summary>
    public class VfxLibrary : MonoBehaviour
    {
        [Tooltip("RF_Glow shader: additive glow shells for bolts, beams and orbs.")]
        public Material glow;
        [Tooltip("RF_Particle shader: soft additive dots for sparks and embers. Used directly by every particle system.")]
        public Material particle;
        [Tooltip("RF_RingDisc shader: shockwave rings and the whirlwind's ground circle.")]
        public Material ringDisc;
        [Tooltip("RF_Funnel shader: the whirlwind's wind streaks.")]
        public Material funnel;
        [Tooltip("RF_Shield shader: hex-grid shield with ripple.")]
        public Material shield;

        /// <summary>True when every template material and its shader is present and supported on this device.</summary>
        public bool IsComplete
        {
            get
            {
                return Usable(glow) && Usable(particle) && Usable(ringDisc) && Usable(funnel) && Usable(shield);
            }
        }

        private static bool Usable(Material m)
        {
            return m != null && m.shader != null && m.shader.isSupported;
        }

        /// <summary>Makes this the library VfxMaterials builds from.</summary>
        public void Register()
        {
            VfxMaterials.SetLibrary(this);
            if (!IsComplete)
                Debug.LogWarning("[RingFighter] VFX library is missing a material or shader: rebuild the scene (Ring Fighter > Build Training Scene).");
        }
    }
}
