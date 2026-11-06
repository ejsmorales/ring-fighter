using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Recharge stance look: a pulsing green glow around the raised ring and a column of light rising from it while
    /// the stance is held. Visual only: RechargeMove does the recharging.
    /// </summary>
    public class RechargeVfx : MonoBehaviour
    {
        public RechargeMove recharge;
        public RingController controller;

        private Transform root;
        private Transform glow;
        private Transform column;
        private Material glowMat;
        private Material columnMat;
        private float strength;

        private void Build()
        {
            root = new GameObject("Recharge FX").transform;
            glowMat = VfxMaterials.NewGlow(VfxMaterials.Green, 1.4f, 1.8f, 0f, 9f, 0.3f);
            columnMat = VfxMaterials.NewFunnel(VfxMaterials.Green, 0.8f, 8f, 1.2f, 3.5f);
            glow = Fx.Shape(PrimitiveType.Sphere, glowMat, Vector3.one * 0.25f, root).transform;
            column = VfxMeshes.CreateFunnel("Recharge Column", columnMat, root).transform;
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
            root.position = hand.transform.position;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 10f);
            glow.localScale = Vector3.one * (0.22f * strength * pulse);
            column.localScale = new Vector3(0.12f, 1.6f * strength, 0.12f);
            if (glowMat != null) glowMat.SetFloat(VfxIds.AlphaId, strength);
            if (columnMat != null) columnMat.SetFloat(VfxIds.AlphaId, strength * 0.7f);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (glowMat != null) Destroy(glowMat);
            if (columnMat != null) Destroy(columnMat);
        }
    }
}
