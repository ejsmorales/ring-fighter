using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Hit feedback built from code, no art needed: a short burst at the impact point, a floating damage
    /// number, and a generated "thump" sound. Call Impact.Hit(...) when something takes damage.
    /// </summary>
    public class Impact : MonoBehaviour
    {
        public enum Kind { Burst, Number }

        private Kind kind;
        private float age;
        private float life;
        private float startScale;
        private Transform viewer;
        private TextMesh textMesh;
        private Color baseColor;
        private Renderer rend;
        private MaterialPropertyBlock block;

        private static AudioClip hitClip;
        private static Material burstMaterial;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Spawns the burst, the damage number and the sound at 'point'.</summary>
        public static void Hit(Vector3 point, float amount, Color color, Transform viewer, bool sound = true, string suffix = null)
        {
            float size = Mathf.Clamp(0.15f + amount * 0.012f, 0.15f, 0.7f);
            SpawnBurst(point, color, size, true);
            SpawnNumber(point + Vector3.up * 0.25f, amount, color, viewer, suffix);
            if (sound) PlayHit(point, amount);
        }

        /// <summary>Just the burst and sound, no number (shield hits, player hits).</summary>
        public static void Spark(Vector3 point, Color color, float size = 0.25f, bool sound = true)
        {
            SpawnBurst(point, color, size);
            if (sound) PlayHit(point, 20f);
        }

        private static void SpawnBurst(Vector3 point, Color color, float size, bool toGreen = false)
        {
            // Glow effects first. A very dark colour (the clash burst) can't show up when added to the scene, so it
            // gets a bright flash and sparks AND the old dark burst below.
            bool dark = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) < 0.3f;
            Color fxColor = dark ? new Color(1f, 0.9f, 0.6f) : color;
            bool drawn = BurstFx.PlayImpact(point, fxColor, size, toGreen && !dark);
            if (drawn && !dark) return;

            GameObject go = Fx.Shape(PrimitiveType.Sphere, GetBurstMaterial(), Vector3.one * 0.05f);
            go.name = "Impact Burst";
            go.transform.position = point;
            Impact i = go.AddComponent<Impact>();
            i.kind = Kind.Burst;
            i.life = 0.25f;
            i.startScale = size;
            i.baseColor = color;
            i.rend = go.GetComponent<Renderer>();
            i.block = new MaterialPropertyBlock();
        }

        private static void SpawnNumber(Vector3 point, float amount, Color color, Transform viewer, string suffix)
        {
            GameObject go = new GameObject("Damage Number");
            go.transform.position = point + Random.insideUnitSphere * 0.08f;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString() + (suffix ?? "");
            Fx.SetupText(tm, 0.1f + Mathf.Min(amount, 60f) * 0.002f, color);
            Impact i = go.AddComponent<Impact>();
            i.kind = Kind.Number;
            i.life = 0.8f;
            i.viewer = viewer;
            i.textMesh = tm;
            i.baseColor = color;
        }

        private static Material GetBurstMaterial()
        {
            if (burstMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Sprites/Default");
                burstMaterial = new Material(s);
            }
            return burstMaterial;
        }

        private static void PlayHit(Vector3 point, float amount)
        {
            if (hitClip == null) hitClip = MakeThump();
            AudioSource.PlayClipAtPoint(hitClip, point, Mathf.Clamp(0.5f + amount * 0.01f, 0.5f, 1f));
        }

        // A short falling-pitch thump with a noise crack on top, generated in code.
        private static AudioClip MakeThump()
        {
            int rate = 44100;
            int n = (int)(rate * 0.18f);
            float[] data = new float[n];
            System.Random rng = new System.Random(7);
            float phase = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n;
                float freq = Mathf.Lerp(220f, 55f, t);
                phase += freq / rate;
                float env = Mathf.Pow(1f - t, 2.2f);
                float tone = Mathf.Sin(phase * 2f * Mathf.PI);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Pow(1f - t, 6f);
                data[k] = (tone * 0.8f + noise * 0.5f) * env;
            }
            AudioClip clip = AudioClip.Create("Hit Thump", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            if (kind == Kind.Burst)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(0.05f, startScale, 1f - (1f - t) * (1f - t));
                Color c = baseColor;
                c.a = 1f - t;
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                rend.SetPropertyBlock(block);
            }
            else
            {
                transform.position += Vector3.up * (0.6f * Time.deltaTime);
                if (viewer != null)
                {
                    Vector3 away = transform.position - viewer.position;
                    if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
                }
                Color c = baseColor;
                c.a = 1f - t * t;
                textMesh.color = c;
            }
        }
    }
}
