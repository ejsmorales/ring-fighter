using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// One-shot hit effects, pooled so nothing is created while fighting. Two styles:
    ///   Impact    - white-hot flash, a shockwave ring facing you and a spray of sparks (every hit).
    ///   Explosion - the same plus an expanding glow shell and a ground ring (blast hit, miss or floor).
    /// Visual only: nothing here touches damage or gameplay.
    /// </summary>
    public class BurstFx : MonoBehaviour
    {
        private enum Style { Impact, Explosion }

        private const int ImpactPoolSize = 8;
        private const int ExplosionPoolSize = 2;

        private static readonly List<BurstFx> impactPool = new List<BurstFx>();
        private static readonly List<BurstFx> explosionPool = new List<BurstFx>();
        private static int impactNext;
        private static int explosionNext;

        private Style style;
        private Material flashMat;
        private Material shellMat;
        private Material ringMat;
        private Material groundMat;
        private Transform flash;
        private Transform shell;
        private Transform ring;
        private Transform ground;
        private ParticleSystem sparks;

        private bool playing;
        private float age;
        private float life;
        private float flashLife;
        private float size;
        private bool useGround;

        // ---- Public entry points ----

        /// <summary>
        /// Flash, ring and sparks at 'point'. 'size' is roughly the burst's diameter in meters (0.15 to 0.7).
        /// 'toGreen' pulls the colour toward ring green (used for your own hits so they read as yours).
        /// Returns false if the effects aren't available, so the caller can fall back to its own visual.
        /// </summary>
        public static bool PlayImpact(Vector3 point, Color color, float size, bool toGreen = false)
        {
            BurstFx fx = Take(impactPool, ref impactNext, ImpactPoolSize, Style.Impact);
            if (fx == null) return false;
            if (toGreen) color = Color.Lerp(color, VfxMaterials.Green, 0.45f);
            fx.Play(point, color, size);
            return true;
        }

        /// <summary>A big green blast explosion. 'diameter' is in meters (about 1 to 2.5).</summary>
        public static bool PlayExplosion(Vector3 point, float diameter)
        {
            BurstFx fx = Take(explosionPool, ref explosionNext, ExplosionPoolSize, Style.Explosion);
            if (fx == null) return false;
            fx.Play(point, VfxMaterials.Green, diameter);
            return true;
        }

        // ---- Pool ----

        private static BurstFx Take(List<BurstFx> pool, ref int next, int poolSize, Style style)
        {
            if (!VfxMaterials.Ready) return null;
            VfxSystem system = VfxSystem.Instance;
            if (system == null) return null;

            if (pool.Count < poolSize)
            {
                BurstFx made = Create(style, system.transform);
                if (made == null) return null;
                pool.Add(made);
                return made;
            }

            if (next >= pool.Count) next = 0;
            BurstFx fx = pool[next];
            if (fx == null)
            {
                fx = Create(style, system.transform);   // destroyed with a scene change: rebuild
                pool[next] = fx;
            }
            next = (next + 1) % pool.Count;
            return fx;
        }

        private static BurstFx Create(Style style, Transform parent)
        {
            GameObject go = new GameObject(style == Style.Impact ? "Impact FX" : "Explosion FX");
            go.transform.SetParent(parent, false);
            BurstFx fx = go.AddComponent<BurstFx>();
            fx.Build(style);
            return fx;
        }

        private void Build(Style s)
        {
            style = s;
            bool explosion = s == Style.Explosion;

            flashMat = VfxMaterials.NewGlow(VfxMaterials.HotWhite, 1.8f, 1.1f);
            ringMat = VfxMaterials.NewRing(VfxMaterials.Green, 1.5f, 0.86f, 0.07f);
            if (flashMat == null || ringMat == null) return;

            flash = Fx.Shape(PrimitiveType.Sphere, flashMat, Vector3.one, transform).transform;
            ring = Fx.Shape(PrimitiveType.Quad, ringMat, Vector3.one, transform).transform;

            if (explosion)
            {
                shellMat = VfxMaterials.NewGlow(VfxMaterials.Green, 1.1f, 1.7f);
                groundMat = VfxMaterials.NewRing(VfxMaterials.Green, 1.3f, 0.82f, 0.09f, 0.35f, 7f, 1.2f, 1.5f);
                if (shellMat != null) shell = Fx.Shape(PrimitiveType.Sphere, shellMat, Vector3.one, transform).transform;
                if (groundMat != null) ground = Fx.Shape(PrimitiveType.Quad, groundMat, Vector3.one, transform).transform;
            }

            sparks = VfxParticles.Create("Sparks", transform, explosion ? 64 : 48, true);
            ParticleSystem.MainModule main = sparks.main;
            main.gravityModifier = explosion ? 0.45f : 0.6f;

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (flashMat != null) Destroy(flashMat);
            if (shellMat != null) Destroy(shellMat);
            if (ringMat != null) Destroy(ringMat);
            if (groundMat != null) Destroy(groundMat);
        }

        // ---- Playing ----

        private void Play(Vector3 point, Color color, float burstSize)
        {
            if (flash == null || ring == null) return;

            size = burstSize;
            age = 0f;
            playing = true;
            bool explosion = style == Style.Explosion;
            life = explosion ? 0.85f : 0.42f;
            flashLife = explosion ? 0.24f : 0.14f;

            transform.position = point;
            flashMat.SetColor(VfxIds.ColorId, Color.Lerp(color, VfxMaterials.HotWhite, 0.6f));
            ringMat.SetColor(VfxIds.ColorId, color);
            if (shellMat != null) shellMat.SetColor(VfxIds.ColorId, color);
            if (groundMat != null) groundMat.SetColor(VfxIds.ColorId, color);

            useGround = explosion && ground != null && point.y < 0.7f;
            SetVisible(true);

            float scale = VfxSettings.ParticleScale;
            if (scale > 0f && sparks != null)
            {
                ParticleSystem.MainModule main = sparks.main;
                int count;
                if (explosion)
                {
                    float k = Mathf.Clamp(burstSize / 1.5f, 0.6f, 1.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * k, 7.5f * k);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                    count = Mathf.RoundToInt(34f * scale);
                }
                else
                {
                    float size01 = Mathf.InverseLerp(0.15f, 0.7f, burstSize);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f + burstSize * 3f, 3.5f + burstSize * 8f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
                    count = Mathf.RoundToInt(Mathf.Lerp(8f, 22f, size01) * scale);
                }
                VfxParticles.Burst(sparks, count, Color.Lerp(color, VfxMaterials.HotWhite, 0.35f));
            }
        }

        private void SetVisible(bool visible)
        {
            SetRenderer(flash, visible);
            SetRenderer(ring, visible);
            SetRenderer(shell, visible);
            SetRenderer(ground, visible && useGround);
        }

        private static void SetRenderer(Transform t, bool on)
        {
            if (t == null) return;
            Renderer r = t.GetComponent<Renderer>();
            if (r != null) r.enabled = on;
        }

        private void Update()
        {
            if (!playing) return;

            age += Time.deltaTime;
            float t = age / life;
            if (t >= 1f)
            {
                playing = false;
                SetVisible(false);
                return;
            }

            bool explosion = style == Style.Explosion;
            float ease = 1f - (1f - t) * (1f - t);
            float fade = (1f - t) * (1f - t);

            // The flash is a hot ball that swells and dies quickly.
            float ft = Mathf.Clamp01(age / flashLife);
            float flashSize = size * (explosion ? 0.7f : 1.2f) * Mathf.Lerp(0.7f, 1.5f, ft);
            flash.localScale = Vector3.one * flashSize;
            flashMat.SetFloat(VfxIds.AlphaId, (1f - ft) * (1f - ft));

            // The shockwave ring always faces you.
            Camera cam = Camera.main;
            Vector3 toCam = cam != null ? cam.transform.position - transform.position : Vector3.back;
            if (toCam.sqrMagnitude < 0.0001f) toCam = Vector3.back;
            ring.rotation = Quaternion.LookRotation(toCam.normalized);
            float ringSize = explosion ? size * Mathf.Lerp(0.25f, 1.6f, ease) : size * Mathf.Lerp(0.3f, 2.4f, ease);
            ring.localScale = new Vector3(ringSize, ringSize, 1f);
            ringMat.SetFloat(VfxIds.AlphaId, fade);

            if (explosion)
            {
                if (shell != null)
                {
                    shell.localScale = Vector3.one * size * Mathf.Lerp(0.25f, 1f, ease);
                    shellMat.SetFloat(VfxIds.AlphaId, fade * 0.9f);
                }
                if (useGround && ground != null)
                {
                    Vector3 p = transform.position;
                    ground.position = new Vector3(p.x, 0.03f, p.z);
                    ground.rotation = Quaternion.Euler(90f, 0f, 0f);
                    float g = size * Mathf.Lerp(0.3f, 2.2f, ease);
                    ground.localScale = new Vector3(g, g, 1f);
                    groundMat.SetFloat(VfxIds.AlphaId, fade);
                }
            }
        }
    }
}
