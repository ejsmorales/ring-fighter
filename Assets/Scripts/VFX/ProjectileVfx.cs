using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws your bolts and charged blasts: a white-hot core inside green glow shells, with a fading tail (bolt) or
    /// a comet tail and embers (blast). Added to every Projectile when it spawns. It hides the placeholder sphere,
    /// follows the projectile, and plays an explosion where a blast ends (target, floor or timing out).
    /// Visual only: the Projectile keeps all damage and hit logic.
    /// </summary>
    public class ProjectileVfx : MonoBehaviour
    {
        /// <summary>How a projectile's life ended (told to this component by Projectile).</summary>
        public enum End { Target, Surface, Expired, Clash }

        private Projectile projectile;
        private Transform root;
        private float radius;
        private bool blast;

        private void Awake()
        {
            projectile = GetComponent<Projectile>();
            if (projectile == null || !VfxMaterials.Ready)
            {
                enabled = false;
                return;
            }

            Renderer placeholder = GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;

            radius = projectile.radius;
            blast = projectile.isBlast;
            Build();
        }

        private Transform Layer(Material mat, Vector3 scale, float z)
        {
            if (mat == null) return null;
            Transform t = Fx.Shape(PrimitiveType.Sphere, mat, scale, root).transform;
            t.localPosition = new Vector3(0f, 0f, z);
            return t;
        }

        private void Build()
        {
            root = new GameObject(blast ? "Blast FX" : "Bolt FX").transform;
            root.SetPositionAndRotation(transform.position, transform.rotation);
            float r = radius;
            bool extra = VfxSettings.ExtraHalos;

            if (!blast)
            {
                // Streak: a long thin glow along the direction of travel, brightest at the front.
                Layer(VfxMaterials.Glow("bolt_core", VfxMaterials.HotWhite, 1.7f, 1.0f), new Vector3(r * 1.6f, r * 1.6f, r * 5f), 0f);
                Layer(VfxMaterials.Glow("bolt_halo", VfxMaterials.Green, 1.1f, 2.0f), new Vector3(r * 4f, r * 4f, r * 9f), 0f);
                if (extra)
                    Layer(VfxMaterials.Glow("bolt_halo2", VfxMaterials.DeepGreen, 0.5f, 2.6f), new Vector3(r * 7f, r * 7f, r * 13f), 0f);
                Layer(VfxMaterials.Glow("bolt_tail", VfxMaterials.Green, 1.0f, 1.3f, 1f), new Vector3(r * 3f, r * 3f, r * 16f), -r * 7f);
            }
            else
            {
                // Sun-orb with a comet tail.
                Layer(VfxMaterials.Glow("blast_core", VfxMaterials.HotWhite, 2.0f, 0.9f), Vector3.one * (r * 2f), 0f);
                Layer(VfxMaterials.Glow("blast_halo", VfxMaterials.Green, 1.2f, 1.8f, 0f, 16f, 0.25f), Vector3.one * (r * 3.6f), 0f);
                if (extra)
                    Layer(VfxMaterials.Glow("blast_halo2", VfxMaterials.DeepGreen, 0.45f, 2.4f, 0f, 11f, 0.3f), Vector3.one * (r * 5.5f), 0f);
                Layer(VfxMaterials.Glow("blast_tail", VfxMaterials.Green, 1.0f, 1.1f, 1f), new Vector3(r * 3f, r * 3f, r * 20f), -r * 8.5f);

                float scale = VfxSettings.ParticleScale;
                if (scale > 0f)
                {
                    ParticleSystem embers = VfxParticles.Create("Embers", root, 40, false);
                    ParticleSystem.MainModule main = embers.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.startColor = VfxMaterials.Green;
                    ParticleSystem.ShapeModule shape = embers.shape;
                    shape.radius = r * 0.8f;
                    VfxParticles.SetRate(embers, 55f * scale);
                }
            }
        }

        private void LateUpdate()
        {
            if (root != null) root.SetPositionAndRotation(transform.position, transform.rotation);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        /// <summary>Called by Projectile just before it is destroyed. Plays the end-of-flight effect.</summary>
        public void OnEnded(End kind, Vector3 point)
        {
            if (root == null) return;

            if (blast)
            {
                if (kind == End.Clash) return;   // the clash burst is already drawn by the enemy bolt
                float diameter = Mathf.Lerp(1.1f, 2.4f, Mathf.InverseLerp(0.1f, 0.32f, radius));
                BurstFx.PlayExplosion(point, diameter);
            }
            else if (kind == End.Surface)
            {
                BurstFx.PlayImpact(point, VfxMaterials.Green, 0.22f);
            }
        }
    }
}
