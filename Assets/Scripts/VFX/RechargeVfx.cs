using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Recharge stance look: the ring in front of your face shines with a bright pulsing green glow (no beam, so it
    /// never blocks your view). Visual only: RechargeMove does the recharging.
    /// </summary>
    public class RechargeVfx : MonoBehaviour
    {
        public RechargeMove recharge;
        public RingController controller;

        private Transform root;
        private Transform glow;
        private Material glowMat;
        private float strength;

        private void Build()
        {
            root = new GameObject("Recharge FX").transform;
            glowMat = VfxMaterials.NewGlow(VfxMaterials.Green, 1.4f, 1.8f, 0f, 9f, 0.3f);
            glow = Fx.Shape(PrimitiveType.Sphere, glowMat, Vector3.one * 0.25f, root).transform;
            root.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (recharge == null || controller == null || controller.Context == null) return;
            bool on = recharge.IsActive;
            strength = Mathf.MoveTowards(strength, on ? 1f : 0f, Time.deltaTime * 4f);
            if (strength <= 0f)
            {
                if (root != null) root.gameObject.SetActive(false);
                return;
            }
            if (root == null)
            {
                if (!VfxMaterials.Ready) return;
                Build();
            }
            TrackedNode hand = controller.Context.RingHand;
            if (hand == null) return;

            root.gameObject.SetActive(true);
            Transform gem = hand.transform.Find("Ring Gem");
            root.position = gem != null ? gem.position : hand.transform.position;
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 10f);
            glow.localScale = Vector3.one * (0.14f * strength * pulse);
            if (glowMat != null) glowMat.SetFloat(VfxIds.AlphaId, strength);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (glowMat != null) Destroy(glowMat);
        }
    }
}
