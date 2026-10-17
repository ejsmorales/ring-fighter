using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// A small floating bar that hovers above your left hand and shows the ring's energy.
    /// Look at your hand to check it; it turns to face your head.
    /// </summary>
    public class EnergyBarVisual : MonoBehaviour
    {
        public RingEnergy energy;
        public Transform follow;     // the hand to hover above
        public Transform lookAt;     // the head
        public Vector3 offset = new Vector3(0f, 0.14f, 0f);
        public Material fillMaterial;
        public Material backMaterial;
        public float width = 0.16f;

        private Transform fill;

        private void Awake()
        {
            Fx.Shape(PrimitiveType.Cube, backMaterial, new Vector3(width + 0.008f, 0.02f, 0.006f), transform);
            GameObject f = Fx.Shape(PrimitiveType.Cube, fillMaterial, new Vector3(width, 0.014f, 0.006f), transform);
            f.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            fill = f.transform;
        }

        private void LateUpdate()
        {
            if (follow != null)
                transform.position = follow.position + offset;

            if (lookAt != null)
            {
                Vector3 away = transform.position - lookAt.position;
                if (away.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(away);
            }

            if (energy != null && fill != null)
            {
                float f = Mathf.Clamp01(energy.Fraction);
                Vector3 s = fill.localScale;
                s.x = Mathf.Max(0.0001f, width * f);
                fill.localScale = s;
                Vector3 p = fill.localPosition;
                p.x = -width * 0.5f * (1f - f);
                fill.localPosition = p;
            }
        }
    }
}
