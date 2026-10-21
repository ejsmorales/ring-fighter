using UnityEngine;
using UnityEngine.Rendering;

namespace RingFighter
{
    /// <summary>
    /// Builds small particle systems from code (sparks, embers) that are cheap enough for Quest 3: world-space,
    /// additive, one shared material, a few dozen particles at most. Systems stay alive and are driven with Emit
    /// (one-shot bursts) or by setting the emission rate (continuous trails), so nothing is created per hit.
    /// </summary>
    public static class VfxParticles
    {
        /// <summary>
        /// Creates a stopped-emission particle system under 'parent'. 'stretch' draws each particle as a streak
        /// along its velocity (sparks). The caller tunes lifetime, speed, size and shape through ps.main / ps.shape.
        /// </summary>
        public static ParticleSystem Create(string name, Transform parent, int maxParticles, bool stretch)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = FadeOut();

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = VfxMaterials.SharedParticle;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (stretch)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.lengthScale = 1.4f;
                r.velocityScale = 0.05f;
            }

            ps.Play();
            return ps;
        }

        /// <summary>White that fades to nothing over the particle's life (the start colour does the tinting).</summary>
        public static Gradient FadeOut()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Emits 'count' particles tinted 'color' from the system's current position and shape.</summary>
        public static void Burst(ParticleSystem ps, int count, Color color)
        {
            if (ps == null || count <= 0) return;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
            ep.startColor = color;
            ps.Emit(ep, count);
        }

        /// <summary>Sets a continuous emission rate (particles per second); 0 stops new particles.</summary>
        public static void SetRate(ParticleSystem ps, float perSecond)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = perSecond;
        }
    }
}
