using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The enemy teleport's look and sound. During the warning: he flickers (TrainingDummy does that), yellow sparks
    /// burst where he stands, and a yellow ring with a rising column of light grows at the spot where he will appear,
    /// with a rising shimmer sound coming from that spot (3D, so you can hear which side). When he jumps: sparks at
    /// both spots and a pop at the new one. Visual/audio only: TrainingDummy and EnemyBrain do the teleport itself.
    /// </summary>
    [RequireComponent(typeof(TrainingDummy))]
    public class TeleportVfx : MonoBehaviour
    {
        public Color color = new Color(1f, 0.8f, 0.2f);
        [Tooltip("Volume of the teleport sounds (0-1).")]
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("Size (meters across) of the warning ring at the destination.")]
        public float markerSize = 1.6f;
        [Tooltip("Height (meters) of the warning column at the destination.")]
        public float markerHeight = 2.4f;

        private TrainingDummy dummy;
        private static AudioClip shimmerClip;
        private static AudioClip popClip;

        private Transform marker;
        private Transform ring;
        private Transform column;
        private Material ringMat;
        private Material columnMat;
        private bool fancy;
        private float markerStart = -1f;
        private float markerEnd = -1f;

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
            if (marker != null) marker.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (marker != null) Destroy(marker.gameObject);
            if (ringMat != null) Destroy(ringMat);
            if (columnMat != null) Destroy(columnMat);
        }

        // The destination marker: the effects shaders if available, else plain yellow shapes.
        private void BuildMarker()
        {
            marker = new GameObject("Teleport Marker").transform;
            fancy = VfxMaterials.Ready;
            if (fancy)
            {
                ringMat = VfxMaterials.NewRing(color, 1.4f, 0.85f, 0.08f, 0.25f, 4f, 1.2f, 2.5f);
                columnMat = VfxMaterials.NewFunnel(color, 0.9f, 7f, 1.5f, 3f);
                ring = Fx.Shape(PrimitiveType.Quad, ringMat, Vector3.one, marker).transform;
                ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
                column = VfxMeshes.CreateFunnel("Teleport Column", columnMat, marker).transform;
            }
            else
            {
                ring = Fx.Shape(PrimitiveType.Cylinder, dummy.attackMaterial, Vector3.one, marker).transform;
                column = Fx.Shape(PrimitiveType.Cylinder, dummy.attackMaterial, Vector3.one, marker).transform;
            }
            ring.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.gameObject.SetActive(false);
        }

        private void OnStarted(Vector3 from, Vector3 to)
        {
            Impact.Spark(from + Vector3.up * dummy.chestHeight, color, 0.5f, false);

            // Built on first use, after the effects system has started (so the effect shaders are ready).
            if (marker == null) BuildMarker();
            marker.position = new Vector3(to.x, dummy.FloorHeight, to.z);
            marker.gameObject.SetActive(true);
            markerStart = Time.time;
            markerEnd = Time.time + dummy.teleportShimmerTime + dummy.teleportGapTime + 0.25f;
            UpdateMarker();

            if (shimmerClip == null) shimmerClip = MakeShimmer(dummy.teleportShimmerTime);
            AudioSource.PlayClipAtPoint(shimmerClip, to + Vector3.up * dummy.chestHeight, volume);
        }

        private void OnTeleported(Vector3 from, Vector3 to)
        {
            Impact.Spark(from + Vector3.up * dummy.chestHeight, color, 0.6f, false);
            Impact.Spark(to + Vector3.up * dummy.chestHeight, color, 0.6f, false);
            if (popClip == null) popClip = MakePop();
            AudioSource.PlayClipAtPoint(popClip, to + Vector3.up * dummy.chestHeight, volume);
        }

        private void Update()
        {
            if (marker == null || !marker.gameObject.activeSelf) return;
            if (Time.time >= markerEnd || !dummy.IsAlive)
            {
                marker.gameObject.SetActive(false);
                return;
            }
            UpdateMarker();
        }

        // Grows in over the warning, fades out right after he appears.
        private void UpdateMarker()
        {
            float warn = Mathf.Max(0.05f, dummy.teleportShimmerTime);
            float grow = Mathf.Clamp01((Time.time - markerStart) / warn);
            float fadeOut = Mathf.Clamp01((markerEnd - Time.time) / 0.25f);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 18f);
            float size = markerSize * Mathf.Lerp(0.3f, 1f, grow) * pulse;

            if (fancy)
            {
                ring.localScale = new Vector3(size, size, 1f);
                column.localScale = new Vector3(size * 0.28f, markerHeight * grow, size * 0.28f);
                float alpha = Mathf.Lerp(0.25f, 1f, grow) * fadeOut;
                if (ringMat != null) ringMat.SetFloat(VfxIds.AlphaId, alpha);
                if (columnMat != null) columnMat.SetFloat(VfxIds.AlphaId, alpha * 0.8f);
            }
            else
            {
                ring.localScale = new Vector3(size, 0.01f, size);
                column.localScale = new Vector3(0.06f, markerHeight * 0.5f * grow, 0.06f);
                column.localPosition = new Vector3(0f, markerHeight * 0.5f * grow, 0f);
            }
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
