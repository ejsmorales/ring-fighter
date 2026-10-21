using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws the charged blast while it is charging between your hands: a white-hot core in pulsing green shells
    /// that swell and throb faster as the charge builds, with sparks pulled inward. A ring flashes when it reaches
    /// full charge. ChargedBlastMove still handles the charge, energy and firing; the flying blast is drawn by
    /// ProjectileVfx.
    /// </summary>
    public class BlastChargeVfx : MonoBehaviour
    {
        private ChargedBlastMove blast;
        private Transform root;
        private Transform core;
        private Transform halo1;
        private Transform halo2;
        private ParticleSystem inflow;
        private Renderer[] renderers;
        private bool visible;
        private bool wasFull;

        public void Init(ChargedBlastMove b)
        {
            blast = b;
            Build();
        }

        private void Build()
        {
            root = new GameObject("Blast Charge FX").transform;

            core = Fx.Shape(PrimitiveType.Sphere, VfxMaterials.Glow("charge_core", VfxMaterials.HotWhite, 2.0f, 0.9f), Vector3.one, root).transform;
            halo1 = Fx.Shape(PrimitiveType.Sphere, VfxMaterials.Glow("charge_halo1", VfxMaterials.Green, 1.3f, 1.7f), Vector3.one, root).transform;
            halo2 = Fx.Shape(PrimitiveType.Sphere, VfxMaterials.Glow("charge_halo2", VfxMaterials.DeepGreen, 0.5f, 2.3f), Vector3.one, root).transform;

            inflow = VfxParticles.Create("Charge Sparks", root, 48, true);
            ParticleSystem.MainModule main = inflow.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.45f);
            main.startSpeed = -1.1f;   // negative: from the outer shell inward
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            main.startColor = Color.Lerp(VfxMaterials.Green, VfxMaterials.HotWhite, 0.4f);
            ParticleSystem.ShapeModule shape = inflow.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            shape.radiusThickness = 0f;   // emit only from the surface

            renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            SetVisible(false);
        }

        private void SetVisible(bool on)
        {
            if (visible == on || renderers == null) return;
            visible = on;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = on;
        }

        private void LateUpdate()
        {
            if (blast == null || root == null) return;

            Transform orb = blast.OrbPlaceholder;
            if (orb == null)
            {
                SetVisible(false);
                VfxParticles.SetRate(inflow, 0f);
                wasFull = false;
                return;
            }

            Renderer placeholder = orb.GetComponent<Renderer>();
            if (placeholder != null && placeholder.enabled) placeholder.enabled = false;

            SetVisible(true);
            float charge = blast.Charge01;
            float d = orb.localScale.x;   // the placeholder's diameter follows the charge
            float throb = Mathf.Lerp(6f, 24f, charge);
            float p1 = 1f + 0.08f * Mathf.Sin(Time.time * throb);
            float p2 = 1f + 0.12f * Mathf.Sin(Time.time * throb * 0.7f + 1.3f);

            root.position = orb.position;
            core.localScale = Vector3.one * (d * 0.8f);
            halo1.localScale = Vector3.one * (d * 1.55f * p1);
            bool extra = VfxSettings.ExtraHalos;
            halo2.gameObject.SetActive(extra);
            if (extra) halo2.localScale = Vector3.one * (d * 2.4f * p2);

            VfxParticles.SetRate(inflow, (25f + 70f * charge) * VfxSettings.ParticleScale);

            bool full = charge >= 0.995f;
            if (full && !wasFull) BurstFx.PlayImpact(orb.position, VfxMaterials.Green, 0.45f);
            if (full) wasFull = true;
            else if (charge < 0.5f) wasFull = false;
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }
    }
}
