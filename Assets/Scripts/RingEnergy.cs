using UnityEngine;

namespace RingFighter
{
    /// <summary>The ring's power meter. Moves spend it; it recharges slowly when idle.</summary>
    public class RingEnergy : MonoBehaviour
    {
        public float max = 100f;
        public float regenPerSecond = 14f;
        [Tooltip("Seconds after spending energy before it starts recharging.")]
        public float regenDelay = 0.8f;

        public float Current { get; private set; }
        public float Fraction { get { return max > 0f ? Current / max : 0f; } }

        private float lastSpendTime = -100f;

        private void Awake()
        {
            Current = max;
        }

        /// <summary>Fill the meter (used when a fight restarts).</summary>
        public void Refill()
        {
            Current = max;
        }

        public bool CanAfford(float cost)
        {
            return Current >= cost;
        }

        /// <summary>Spend a one-off cost. Returns false (and spends nothing) if there isn't enough.</summary>
        public bool TrySpend(float cost)
        {
            if (Current < cost) return false;
            Current -= cost;
            lastSpendTime = Time.time;
            return true;
        }

        /// <summary>Spend energy continuously. Returns false once the meter is empty.</summary>
        public bool Drain(float perSecond)
        {
            if (Current <= 0f) return false;
            Current = Mathf.Max(0f, Current - perSecond * Time.deltaTime);
            lastSpendTime = Time.time;
            return Current > 0f;
        }

        private void Update()
        {
            if (Time.time - lastSpendTime >= regenDelay)
                Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime);
        }
    }
}
