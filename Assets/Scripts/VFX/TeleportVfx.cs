using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The enemy teleport's look and sound: a rising shimmer sound and yellow sparks while he flickers (the warning),
    /// then a pop of sparks where he left and where he reappears. The sound is 3D, so you can hear which side he
    /// went to. Visual/audio only: TrainingDummy and EnemyBrain do the teleport itself.
    /// </summary>
    [RequireComponent(typeof(TrainingDummy))]
    public class TeleportVfx : MonoBehaviour
    {
        public Color color = new Color(1f, 0.8f, 0.2f);
        [Tooltip("Volume of the teleport sounds (0-1).")]
        [Range(0f, 1f)] public float volume = 0.9f;

        private TrainingDummy dummy;
        private static AudioClip shimmerClip;
        private static AudioClip popClip;

        private void Awake()
        {
            dummy = GetComponent<TrainingDummy>();
        }

        private void OnEnable()
        {
            if (dummy == null) dummy = GetComponent<TrainingDummy>();
            dummy.TeleportStarted += OnStarted;
            dummy.Teleported += OnTeleported;
        }

        private void OnDisable()
        {
            dummy.TeleportStarted -= OnStarted;
            dummy.Teleported -= OnTeleported;
        }

        private void OnStarted(Vector3 from)
        {
            Vector3 chest = from + Vector3.up * (dummy.chestHeight);
            Impact.Spark(chest, color, 0.5f, false);
            if (shimmerClip == null) shimmerClip = MakeShimmer(dummy.teleportShimmerTime);
            AudioSource.PlayClipAtPoint(shimmerClip, chest, volume);
        }

        private void OnTeleported(Vector3 from, Vector3 to)
        {
            Impact.Spark(from + Vector3.up * dummy.chestHeight, color, 0.6f, false);
            Impact.Spark(to + Vector3.up * dummy.chestHeight, color, 0.6f, false);
            if (popClip == null) popClip = MakePop();
            AudioSource.PlayClipAtPoint(popClip, to + Vector3.up * dummy.chestHeight, volume);
        }

        // A rising, warbling tone that lasts as long as the warning.
        private static AudioClip MakeShimmer(float seconds)
        {
            int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * Mathf.Max(0.2f, seconds)));
            float[] data = new float[n];
            float phase = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n;
                float freq = Mathf.Lerp(300f, 1400f, t * t) * (1f + 0.04f * Mathf.Sin(k * 0.004f));
                phase += freq / rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.2f)) * 0.5f + 0.2f * t;
                data[k] = Mathf.Sin(phase * 2f * Mathf.PI) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("Teleport Shimmer", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // A short airy pop where he reappears.
        private static AudioClip MakePop()
        {
            int rate = 44100;
            int n = (int)(rate * 0.22f);
            float[] data = new float[n];
            System.Random rng = new System.Random(11);
            float phase = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n;
                phase += Mathf.Lerp(900f, 200f, t) / rate;
                float env = Mathf.Pow(1f - t, 3f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[k] = (Mathf.Sin(phase * 2f * Mathf.PI) * 0.6f + noise * 0.4f) * env;
            }
            AudioClip clip = AudioClip.Create("Teleport Pop", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
