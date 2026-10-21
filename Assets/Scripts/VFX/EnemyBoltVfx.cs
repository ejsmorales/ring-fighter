using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws the dummy's bolt: the same streak shape as yours but yellow (orange when it is aimed at your head),
    /// with a hostile flicker, a hot white core and an ember trail. A reflected bolt turns your ring green and
    /// stops flickering. Added to every EnemyBolt when it spawns; the EnemyBolt keeps all the gameplay.
    /// </summary>
    public class EnemyBoltVfx : MonoBehaviour
    {
        private const int Yellow = 0;
        private const int Orange = 1;
        private const int Green = 2;

        private static readonly Material[][] sets = new Material[3][];

        private EnemyBolt bolt;
        private Transform root;
        private readonly Renderer[] layers = new Renderer[4];   // core, halo, outer halo, tail
        private ParticleSystem embers;
        private int palette = -1;

        private void Awake()
        {
            bolt = GetComponent<EnemyBolt>();
            if (bolt == null || !VfxMaterials.Ready)
            {
                enabled = false;
                return;
            }

            Renderer placeholder = GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;
            Build();
            Apply(WantedPalette());
        }

        private Transform Layer(int index, Vector3 scale, float z)
        {
            Transform t = Fx.Shape(PrimitiveType.Sphere, null, scale, root).transform;
            t.localPosition = new Vector3(0f, 0f, z);
            layers[index] = t.GetComponent<Renderer>();
            return t;
        }

        private void Build()
        {
            root = new GameObject("Enemy Bolt FX").transform;
            root.SetPositionAndRotation(transform.position, transform.rotation);

            Layer(0, new Vector3(0.07f, 0.07f, 0.28f), 0f);
            Layer(1, new Vector3(0.2f, 0.2f, 0.5f), 0f);
            if (VfxSettings.ExtraHalos) Layer(2, new Vector3(0.34f, 0.34f, 0.8f), 0f);
            Layer(3, new Vector3(0.11f, 0.11f, 0.9f), -0.45f);

            float scale = VfxSettings.ParticleScale;
            if (scale > 0f)
            {
                embers = VfxParticles.Create("Embers", root, 30, false);
                ParticleSystem.MainModule main = embers.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
                ParticleSystem.ShapeModule shape = embers.shape;
                shape.radius = 0.06f;
                VfxParticles.SetRate(embers, 40f * scale);
            }
        }

        private int WantedPalette()
        {
            if (bolt.IsReflected) return Green;
            return bolt.HeadShot ? Orange : Yellow;
        }

        private static Material[] GetSet(int p)
        {
            if (sets[p] != null && sets[p][0] != null) return sets[p];

            Color c;
            string name;
            float pulseSpeed;
            float pulseAmount;
            switch (p)
            {
                case Orange: c = VfxMaterials.EnemyOrange; name = "orange"; pulseSpeed = 32f; pulseAmount = 0.45f; break;
                case Green: c = VfxMaterials.Green; name = "green"; pulseSpeed = 0f; pulseAmount = 0f; break;
                default: c = VfxMaterials.EnemyYellow; name = "yellow"; pulseSpeed = 22f; pulseAmount = 0.3f; break;
            }

            sets[p] = new Material[]
            {
                VfxMaterials.Glow("enemy_" + name + "_core", Color.Lerp(c, VfxMaterials.HotWhite, 0.7f), 1.7f, 1.0f),
                VfxMaterials.Glow("enemy_" + name + "_halo", c, 1.2f, 1.9f, 0f, pulseSpeed, pulseAmount, 38f, p == Green ? 0f : 0.25f, 14f),
                VfxMaterials.Glow("enemy_" + name + "_outer", c, 0.5f, 2.6f, 0f, pulseSpeed * 0.7f, pulseAmount),
                VfxMaterials.Glow("enemy_" + name + "_tail", c, 1.0f, 1.3f, 1f)
            };
            return sets[p];
        }

        private void Apply(int p)
        {
            palette = p;
            Material[] set = GetSet(p);
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] != null && set[i] != null) layers[i].sharedMaterial = set[i];

            if (embers != null)
            {
                Color c = p == Orange ? VfxMaterials.EnemyOrange : (p == Green ? VfxMaterials.Green : VfxMaterials.EnemyYellow);
                ParticleSystem.MainModule main = embers.main;
                main.startColor = c;
            }
        }

        private void LateUpdate()
        {
            if (root == null) return;
            root.SetPositionAndRotation(transform.position, transform.rotation);
            int want = WantedPalette();
            if (want != palette) Apply(want);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }
    }
}
