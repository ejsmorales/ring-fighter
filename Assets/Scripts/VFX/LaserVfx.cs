using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws the focused laser as a lightsaber-style blade: a hair-thin white-hot core, a bright green body that
    /// shimmers along its length, and a soft outer halo. A flare sits at the ring, and a hot spot with a spray of
    /// sparks sits wherever the beam lands. LaserMove still does all the aiming, damage and energy; this
    /// component only reads the placeholder beam it makes, hides it, and draws over it.
    /// </summary>
    public class LaserVfx : MonoBehaviour
    {
        private LaserMove laser;
        private Transform root;
        private Transform core;
        private Transform inner;
        private Transform outer;
        private Transform muzzle;
        private Transform muzzleHalo;
        private Transform hot;
        private Transform hotHalo;
        private ParticleSystem sparks;
        private Renderer[] renderers;
        private bool visible;

        private float fade;
        private Vector3 lastStart;
        private Vector3 lastEnd;
        private bool hasBeam;
        private bool lastHit;

        public void Init(LaserMove l)
        {
            laser = l;
            Build();
        }

        private Transform Part(PrimitiveType type, Material mat)
        {
            return Fx.Shape(type, mat, Vector3.one, root).transform;
        }

        private void Build()
        {
            root = new GameObject("Laser FX").transform;

            core = Part(PrimitiveType.Cylinder, VfxMaterials.Glow("laser_core", VfxMaterials.HotWhite, 2.2f, 0.9f));
            inner = Part(PrimitiveType.Cylinder, VfxMaterials.Glow("laser_inner", VfxMaterials.Green, 1.4f, 1.7f, 0f, 0f, 0f, 28f, 0.3f, 14f));
            outer = Part(PrimitiveType.Cylinder, VfxMaterials.Glow("laser_outer", VfxMaterials.DeepGreen, 0.5f, 2.3f, 0f, 0f, 0f, 18f, 0.2f, 9f));
            muzzle = Part(PrimitiveType.Sphere, VfxMaterials.Glow("laser_muzzle", VfxMaterials.HotWhite, 1.8f, 1.0f));
            muzzleHalo = Part(PrimitiveType.Sphere, VfxMaterials.Glow("laser_muzzleHalo", VfxMaterials.Green, 1.0f, 2.0f, 0f, 24f, 0.3f));
            hot = Part(PrimitiveType.Sphere, VfxMaterials.Glow("laser_hot", VfxMaterials.HotWhite, 2.0f, 1.0f));
            hotHalo = Part(PrimitiveType.Sphere, VfxMaterials.Glow("laser_hotHalo", VfxMaterials.Green, 1.1f, 1.8f, 0f, 30f, 0.35f));

            sparks = VfxParticles.Create("Hit Sparks", root, 48, true);
            ParticleSystem.MainModule main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.025f);
            main.startColor = Color.Lerp(VfxMaterials.Green, VfxMaterials.HotWhite, 0.4f);
            main.gravityModifier = 0.3f;
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.01f;

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

        private static void HidePlaceholder(Transform t)
        {
            if (t == null) return;
            Renderer r = t.GetComponent<Renderer>();
            if (r != null && r.enabled) r.enabled = false;
        }

        private static void PlaceBeam(Transform t, Vector3 a, Vector3 b, Vector3 dir, float length, float width)
        {
            t.position = (a + b) * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            t.localScale = new Vector3(width, length * 0.5f, width);   // a Unity cylinder is 2 units tall
        }

        private void LateUpdate()
        {
            if (laser == null || root == null) return;

            Transform beam = laser.BeamPlaceholder;
            float dt = Time.unscaledDeltaTime;
            if (beam != null)
            {
                HidePlaceholder(beam);
                HidePlaceholder(laser.TipPlaceholder);

                Vector3 axis = beam.rotation * Vector3.up;
                float half = beam.localScale.y;
                lastStart = beam.position - axis * half;
                lastEnd = beam.position + axis * half;
                lastHit = half * 2f < laser.range - 0.05f;
                hasBeam = true;
                fade = Mathf.MoveTowards(fade, 1f, dt * 14f);
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 0f, dt * 10f);
            }

            if (fade <= 0f || !hasBeam)
            {
                SetVisible(false);
                VfxParticles.SetRate(sparks, 0f);
                return;
            }

            Vector3 delta = lastEnd - lastStart;
            float len = delta.magnitude;
            if (len < 0.001f) return;
            Vector3 dir = delta / len;
            SetVisible(true);

            // Widths grow in and shrink out with 'fade'; beamWidth lets you thicken the whole blade from the move.
            float w = (laser.beamWidth / 0.025f) * Mathf.SmoothStep(0f, 1f, fade);
            PlaceBeam(core, lastStart, lastEnd, dir, len, 0.012f * w);
            PlaceBeam(inner, lastStart, lastEnd, dir, len, 0.05f * w);
            bool extra = VfxSettings.ExtraHalos;
            outer.gameObject.SetActive(extra);
            if (extra) PlaceBeam(outer, lastStart, lastEnd, dir, len, 0.16f * w);

            muzzle.position = lastStart;
            muzzle.localScale = Vector3.one * (0.07f * w);
            muzzleHalo.position = lastStart;
            muzzleHalo.localScale = Vector3.one * (0.2f * w);

            hot.gameObject.SetActive(lastHit);
            hotHalo.gameObject.SetActive(lastHit);
            if (lastHit)
            {
                hot.position = lastEnd;
                hot.localScale = Vector3.one * (0.1f * w);
                hotHalo.position = lastEnd;
                hotHalo.localScale = Vector3.one * (0.3f * w);
            }

            sparks.transform.position = lastEnd;
            sparks.transform.rotation = Quaternion.LookRotation(-dir);
            bool emit = lastHit && beam != null && fade > 0.5f;
            VfxParticles.SetRate(sparks, emit ? 80f * VfxSettings.ParticleScale : 0f);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }
    }
}
