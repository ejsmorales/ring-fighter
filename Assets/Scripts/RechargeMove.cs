using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Recharge stance: squeeze BOTH grips and raise the ring hand above your head, like charging the ring. Energy
    /// refills fast, but while you hold it you can't attack or shield, you move slowly, and the enemy treats it as an
    /// opening. Lower the ring hand or let go of either grip to stop.
    /// </summary>
    public class RechargeMove : Move
    {
        [Tooltip("Energy per second while in the stance.")]
        public float rechargePerSecond = 35f;
        [Tooltip("How hard (0-1) both grips must be squeezed.")]
        [Range(0f, 1f)] public float gripThreshold = 0.7f;
        [Tooltip("To START, the ring hand must be at least this far (meters) above your head.")]
        public float startAboveHead = 0.05f;
        [Tooltip("The stance ends if the ring hand drops more than this far (meters) below head height.")]
        public float stayAboveHead = -0.15f;
        [Tooltip("While in the stance you move at this fraction of your normal speed (dashing is off).")]
        [Range(0f, 1f)] public float moveSpeedFactor = 0.35f;

        private float nextBuzz;

        private void Reset()
        {
            priority = 40;
        }

        private bool GripsHeld()
        {
            TrackedNode l = ctx.Left, r = ctx.Right;
            return l != null && r != null && l.Grip >= gripThreshold && r.Grip >= gripThreshold;
        }

        private float RingHandHeight()
        {
            return ctx.RingHand.transform.position.y - ctx.Head.transform.position.y;
        }

        public override bool TryStart(int freeHands)
        {
            if (freeHands != BothMask || ctx.RingHand == null || ctx.Head == null) return false;
            if (!ctx.RingHand.IsTracked || !GripsHeld()) return false;
            if (RingHandHeight() < startAboveHead) return false;

            IsActive = true;
            UsedHands = BothMask;
            ctx.RingHand.Haptic(0.4f, 0.1f);
            MoveLog.Add("Recharging");
            return true;
        }

        public override void Tick()
        {
            string reason = null;
            if (!GripsHeld()) reason = "grips released";
            else if (!ctx.RingHand.IsTracked || RingHandHeight() < stayAboveHead) reason = "ring hand lowered";
            if (reason != null)
            {
                MoveLog.Add("Recharge ended: " + reason + " (energy " + Mathf.RoundToInt(ctx.energy.Current) + ")");
                Finish();
                return;
            }

            ctx.energy.Add(rechargePerSecond * Time.deltaTime);

            // Soft pulse in the ring hand; it stops when full.
            if (Time.time >= nextBuzz && ctx.energy.Fraction < 0.999f)
            {
                nextBuzz = Time.time + 0.25f;
                ctx.RingHand.Haptic(0.12f, 0.05f);
            }
        }
    }
}
