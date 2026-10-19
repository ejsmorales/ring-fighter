using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The player's heads-up display, built from code: a curved vertical health bar on the left, a curved
    /// vertical ring-energy bar on the right (both bend inward like parentheses framing the view), a red edge
    /// flash when you're hit, and a red fade with "KNOCKED OUT" when health runs out.
    /// The bars soft-follow your head: they lag a little behind head turns instead of being glued to your face,
    /// which is easier on the eyes. The flash and fade are rigidly attached to the head.
    /// </summary>
    public class PlayerHud : MonoBehaviour
    {
        public Transform head;
        public PlayerHealth health;
        public RingEnergy energy;

        [Header("Materials (unlit)")]
        public Material healthMaterial;
        public Material energyMaterial;
        public Material backMaterial;
        [Tooltip("Transparent unlit material for the red flash and fade.")]
        public Material flashMaterial;

        [Header("Layout")]
        [Tooltip("How far in front of the eyes the bars sit (meters).")]
        public float distance = 1.2f;
        [Tooltip("How far left/right of straight ahead (meters at that distance) the bars' outer edge sits.")]
        public float sideOffset = 0.55f;
        [Tooltip("Radius of the curve (meters). Smaller = more curved.")]
        public float arcRadius = 0.9f;
        [Tooltip("Half of the bar's sweep, in degrees. Bigger = taller bar.")]
        public float arcHalfAngle = 28f;
        public float barWidth = 0.035f;
        [Tooltip("How quickly the bars catch up with your head turning. Lower = lazier.")]
        public float followSpeed = 5f;

        private Mesh healthBack, healthFill, energyBack, energyFill;
        private float shownHealth = -1f;
        private float shownEnergy = -1f;
        private bool snapped;

        private Material flashMat, fadeMat;
        private Texture2D vignette;
        private TextMesh knockedOut;
        private readonly List<Vector3> verts = new List<Vector3>(64);
        private readonly List<int> tris = new List<int>(192);

        private const int Segments = 28;

        private void Start()
        {
            healthBack = MakeBar("Health Back", backMaterial, barWidth + 0.014f, 0.004f);
            healthFill = MakeBar("Health Fill", healthMaterial, barWidth, 0f);
            energyBack = MakeBar("Energy Back", backMaterial, barWidth + 0.014f, 0.004f);
            energyFill = MakeBar("Energy Fill", energyMaterial, barWidth, 0f);
            Fill(healthBack, true, 1f, barWidth + 0.014f);
            Fill(energyBack, false, 1f, barWidth + 0.014f);

            if (head != null)
            {
                BuildScreenEffects();
                transform.position = head.position;
            }
        }

        // ---- Bars ----

        private Mesh MakeBar(string name, Material material, float width, float extraDepth)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, distance + extraDepth);
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mesh;
        }

        // Rebuilds a curved strip. 'left' picks which side of the view it hugs. 'fraction' of the arc is filled from the bottom.
        private void Fill(Mesh mesh, bool left, float fraction, float width)
        {
            mesh.Clear();
            if (fraction <= 0.004f) return;

            float half = arcHalfAngle * Mathf.Deg2Rad;
            float side = left ? -1f : 1f;
            float centerX = side * sideOffset - side * arcRadius;   // circle centre sits toward the middle of the view
            int count = Mathf.Max(2, Mathf.CeilToInt(Segments * fraction) + 1);

            verts.Clear();
            tris.Clear();
            for (int i = 0; i < count; i++)
            {
                float u = fraction * i / (count - 1);
                float theta = -half + 2f * half * u;
                float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
                Vector3 pos = new Vector3(centerX + side * arcRadius * c, arcRadius * s, 0f);
                Vector3 toRight = left ? new Vector3(c, -s, 0f) : new Vector3(c, s, 0f);
                verts.Add(pos - toRight * (width * 0.5f));   // left edge
                verts.Add(pos + toRight * (width * 0.5f));   // right edge
            }
            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2;
                // Clockwise as seen from the viewer, so the front faces them.
                tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
                tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
            }
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        // ---- Red flash and fade ----

        private void BuildScreenEffects()
        {
            vignette = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            vignette.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x + 0.5f) / 32f - 1f;
                    float dy = (y + 0.5f) / 32f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.SmoothStep(0.45f, 1.15f, r);
                    vignette.SetPixel(x, y, new Color(1f, 0.1f, 0.05f, a));
                }
            }
            vignette.Apply();

            flashMat = MakeScreenMaterial(vignette, new Color(1f, 1f, 1f, 0f));
            fadeMat = MakeScreenMaterial(Texture2D.whiteTexture, new Color(0.25f, 0f, 0f, 0f));
            MakeScreenQuad("Hit Flash", flashMat, 0.5f, 2.6f);
            MakeScreenQuad("Death Fade", fadeMat, 0.4f, 2.4f);

            GameObject t = new GameObject("Knocked Out Text");
            t.transform.SetParent(head, false);
            t.transform.localPosition = new Vector3(0f, 0.05f, 1.2f);
            knockedOut = t.AddComponent<TextMesh>();
            knockedOut.text = "KNOCKED OUT";
            Fx.SetupText(knockedOut, 0.1f, new Color(1f, 0.5f, 0.45f, 0f));
        }

        private Material MakeScreenMaterial(Texture tex, Color color)
        {
            Material m;
            if (flashMaterial != null) m = new Material(flashMaterial);
            else
            {
                Shader sh = Shader.Find("Sprites/Default");
                m = new Material(sh);
            }
            m.mainTexture = tex;
            m.color = color;
            return m;
        }

        private void MakeScreenQuad(string name, Material mat, float dist, float size)
        {
            GameObject q = Fx.Shape(PrimitiveType.Quad, mat, new Vector3(size, size, 1f), head);
            q.name = name;
            q.transform.localPosition = new Vector3(0f, 0f, dist);
            q.transform.localRotation = Quaternion.identity;
        }

        // ---- Per frame ----

        private void LateUpdate()
        {
            if (head == null) return;

            // Soft follow: always at the head, rotation eases toward where the head faces.
            transform.position = head.position;
            Quaternion target = Quaternion.LookRotation(head.forward, Vector3.up);
            if (!snapped)
            {
                transform.rotation = target;
                snapped = true;
            }
            else
            {
                float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, k);
            }

            float h = health != null ? health.Fraction : 1f;
            if (Mathf.Abs(h - shownHealth) > 0.002f)
            {
                shownHealth = h;
                Fill(healthFill, true, h, barWidth);
            }
            float e = energy != null ? Mathf.Clamp01(energy.Fraction) : 1f;
            if (Mathf.Abs(e - shownEnergy) > 0.002f)
            {
                shownEnergy = e;
                Fill(energyFill, false, e, barWidth);
            }

            float flash = health != null ? health.HitFlash : 0f;
            float fade = health != null ? health.DeathFade : 0f;
            if (flashMat != null) flashMat.color = new Color(1f, 1f, 1f, flash * 0.9f);
            if (fadeMat != null) fadeMat.color = new Color(0.25f, 0f, 0f, fade * 0.92f);
            if (knockedOut != null) knockedOut.color = new Color(1f, 0.5f, 0.45f, Mathf.Clamp01((fade - 0.5f) * 2f));
        }

        private void OnDestroy()
        {
            if (flashMat != null) Destroy(flashMat);
            if (fadeMat != null) Destroy(fadeMat);
            if (vignette != null) Destroy(vignette);
        }
    }
}
