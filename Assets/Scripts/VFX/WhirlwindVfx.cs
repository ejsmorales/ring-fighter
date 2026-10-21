using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws the whirlwind. It travels along the floor as a spinning green energy circle with a low swirling rim.
    /// When it catches the enemy, the rim rises into a tall funnel of wind streaks (with an inner funnel and sparks
    /// spiralling up) and settles back down if the contact ends. WhirlwindMove still does all the movement, damage
    /// and lifting; this only reads its position and whether it is touching something.
    /// </summary>
    public class WhirlwindVfx : MonoBehaviour
    {
        private WhirlwindMove whirl;
        private Transform root;
        private Transform groundCircle;
        private Transform funnel;
        private Transform innerFunnel;
        private Material circleMat;
        private Material funnelMat;
        private Material innerMat;
        private ParticleSystem sparks;
        private Renderer[] renderers;
        private bool visible;

        private float fade;
        private float rise;
        private float lastContact = -10f;
        private Vector3 lastPos;
        private bool hasPos;

        public void Init(WhirlwindMove w)
        {
            whirl = w;
            Build();
        }

        private void Build()
        {
            circleMat = VfxMaterials.NewRing(VfxMaterials.Green, 1.3f, 0.88f, 0.06f, 0.4f, 6f, 1.6f, 3.2f);
            funnelMat = VfxMaterials.NewFunnel(VfxMaterials.Green, 1.0f, 9f, 2.5f, 2.6f);
            innerMat = VfxMaterials.NewFunnel(Color.Lerp(VfxMaterials.Green, VfxMaterials.HotWhite, 0.5f), 0.7f, 6f, 3.2f, 4.2f);
            if (circleMat == null || funnelMat == null || innerMat == null)
            {
                enabled = false;
                return;
            }

            root = new GameObject("Whirlwind FX").transform;

            groundCircle = Fx.Shape(PrimitiveType.Quad, circleMat, Vector3.one, root).transform;
            groundCircle.localPosition = new Vector3(0f, 0.03f, 0f);
            groundCircle.localRotation = Quaternion.Euler(90f, 0f, 0f);

            funnel = VfxMeshes.CreateFunnel("Funnel", funnelMat, root).transform;
            funnel.localPosition = new Vector3(0f, 0.02f, 0f);
            innerFunnel = VfxMeshes.CreateFunnel("Inner Funnel", innerMat, root).transform;
            innerFunnel.localPosition = new Vector3(0f, 0.02f, 0f);

            sparks = VfxParticles.Create("Wind Sparks", root, 60, false);
            ParticleSystem.MainModule main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = Color.Lerp(VfxMaterials.Green, VfxMaterials.HotWhite, 0.45f);
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = whirl.radius * 0.85f;
            shape.radiusThickness = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // lay the circle flat on the floor
            ParticleSystem.VelocityOverLifetimeModule vol = sparks.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(2.2f);
            vol.z = new ParticleSystem.MinMaxCurve(0f);
            vol.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            vol.orbitalY = new ParticleSystem.MinMaxCurve(2.5f);
            vol.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

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
            if (whirl == null || root == null) return;

            Transform rt = whirl.RootTransform;
            bool on = rt != null;
            float dt = Time.deltaTime;

            if (on)
            {
                lastPos = rt.position;
                hasPos = true;
                if (whirl.InContact) lastContact = Time.time;

                // The placeholder discs stay for the move's own bookkeeping; just don't draw them.
                Transform[] discs = whirl.Discs;
                if (discs != null)
                {
                    for (int i = 0; i < discs.Length; i++)
                    {
                        if (discs[i] == null) continue;
                        Renderer r = discs[i].GetComponent<Renderer>();
                        if (r != null && r.enabled) r.enabled = false;
                    }
                }
            }

            fade = Mathf.MoveTowards(fade, on ? 1f : 0f, Time.unscaledDeltaTime * (on ? 6f : 3.5f));
            if (fade <= 0f || !hasPos)
            {
                SetVisible(false);
                VfxParticles.SetRate(sparks, 0f);
                return;
            }
            SetVisible(true);

            // Low rim while travelling, tall funnel while touching the enemy (kept up briefly after contact).
            bool touching = on && (Time.time - lastContact) < 0.45f;
            rise = Mathf.MoveTowards(rise, touching ? 1f : 0f, dt * (on ? 3f : 5f));
            float riseEase = Mathf.SmoothStep(0f, 1f, rise);

            root.position = lastPos;
            float r2 = whirl.radius;

            float circle = r2 * 2.3f;
            groundCircle.localScale = new Vector3(circle, circle, 1f);

            float height = Mathf.Lerp(0.3f, 2.5f, riseEase);
            funnel.localScale = new Vector3(r2, height, r2);
            circleMat.SetFloat(VfxIds.AlphaId, fade);
            funnelMat.SetFloat(VfxIds.AlphaId, fade * Mathf.Lerp(0.55f, 1f, riseEase));

            bool inner = VfxSettings.ExtraHalos && rise > 0.05f;
            innerFunnel.gameObject.SetActive(inner);
            if (inner)
            {
                innerFunnel.localScale = new Vector3(r2 * 0.62f, height * 0.9f, r2 * 0.62f);
                innerMat.SetFloat(VfxIds.AlphaId, fade * riseEase);
            }

            sparks.transform.position = lastPos;
            float rate = (14f + 45f * riseEase) * VfxSettings.ParticleScale * (on ? 1f : 0f);
            VfxParticles.SetRate(sparks, rate);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (circleMat != null) Destroy(circleMat);
            if (funnelMat != null) Destroy(funnelMat);
            if (innerMat != null) Destroy(innerMat);
        }
    }
}
