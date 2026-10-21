using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Draws the shield as a hex-grid barrier with a bright rim. It pops in when raised, a ripple spreads from
    /// wherever a bolt strikes it (and the whole barrier flares), and the double shield is brighter and whiter.
    /// ShieldMove still owns the pose, energy, blocking and reflecting; this reads the placeholder disc it makes,
    /// hides it, and draws over it.
    /// </summary>
    public class ShieldVfx : MonoBehaviour
    {
        private ShieldMove shield;
        private GameObject visual;
        private Material mat;

        private float appear;
        private float rippleAge = 99f;
        private float flash;
        private int lastImpact;
        private bool hasShape;
        private Vector3 lastPos;
        private Quaternion lastRot = Quaternion.identity;
        private Vector3 lastScale = Vector3.one;

        public void Init(ShieldMove s)
        {
            shield = s;
            mat = VfxMaterials.NewShield(VfxMaterials.Green, 1f);
            if (mat == null)
            {
                enabled = false;
                return;
            }
            visual = Fx.Shape(PrimitiveType.Cylinder, mat, Vector3.one);
            visual.name = "Shield FX";
            visual.SetActive(false);
            lastImpact = shield.ImpactCount;
        }

        private void LateUpdate()
        {
            if (shield == null || visual == null) return;

            Transform disc = shield.DiscTransform;
            bool on = shield.IsBlocking && disc != null;
            if (on)
            {
                Renderer placeholder = disc.GetComponent<Renderer>();
                if (placeholder != null && placeholder.enabled) placeholder.enabled = false;
                lastPos = disc.position;
                lastRot = disc.rotation;
                lastScale = disc.localScale;
                hasShape = true;
            }

            float dt = Time.unscaledDeltaTime;
            appear = Mathf.MoveTowards(appear, on ? 1f : 0f, dt * (on ? 8f : 6f));
            if (appear <= 0f || !hasShape)
            {
                if (visual.activeSelf) visual.SetActive(false);
                return;
            }
            if (!visual.activeSelf) visual.SetActive(true);

            float pop = Mathf.Lerp(0.55f, 1f, 1f - (1f - appear) * (1f - appear));
            Transform vt = visual.transform;
            vt.SetPositionAndRotation(lastPos, lastRot);
            vt.localScale = new Vector3(lastScale.x * pop, 0.01f, lastScale.z * pop);

            // A bolt hit the barrier: start a ripple at that spot (in the disc's own -1..1 coordinates).
            if (shield.ImpactCount != lastImpact)
            {
                lastImpact = shield.ImpactCount;
                Vector3 local = vt.InverseTransformPoint(shield.LastImpactPoint);
                mat.SetVector(VfxIds.RippleCenterId, new Vector4(local.x * 2f, local.z * 2f, 0f, 0f));
                rippleAge = 0f;
                flash = 1f;
            }
            rippleAge = Mathf.Min(rippleAge + Time.deltaTime, 99f);
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3f);

            bool doubled = shield.IsDoubled;
            mat.SetColor(VfxIds.ColorId, doubled ? Color.Lerp(VfxMaterials.Green, VfxMaterials.HotWhite, 0.35f) : VfxMaterials.Green);
            mat.SetFloat(VfxIds.IntensityId, doubled ? 1.35f : 1f);
            mat.SetFloat(VfxIds.HexScaleId, lastScale.x * 3.8f);   // about 13 cm hexagons at any size
            mat.SetFloat(VfxIds.RippleAgeId, rippleAge);
            mat.SetFloat(VfxIds.FlashId, flash);
            mat.SetFloat(VfxIds.AlphaId, appear);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
            if (mat != null) Destroy(mat);
        }
    }
}
