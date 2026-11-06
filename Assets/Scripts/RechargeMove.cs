using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Recharge stance: make a fist with the ring hand (squeeze its grip) and bring the ring up in front of your face,
    /// as if looking at it shine. Energy refills fast, but while you hold it you can't attack or shield, you move
    /// slowly, and the enemy treats it as an opening. Let go of the grip or lower the ring to stop.
    /// </summary>
    public class RechargeMove : Move
    {
        [Tooltip("Energy per second while in the stance.")]
        public float rechargePerSecond = 35f;
        [Tooltip("How hard (0-1) the ring hand's grip must be squeezed.")]
        [Range(0f, 1f)] public float gripThreshold = 0.7f;

        [Header("Where the ring must be (relative to your eyes, in meters)")]
        [Tooltip("To START: how far in front of your face the ring may be (min, max).")]
        public Vector2 startForward = new Vector2(0.08f, 0.5f);
        [Tooltip("To START: how far below (-) / above (+) eye level the ring may be (min, max).")]
        public Vector2 startHeight = new Vector2(-0.3f, 0.2f);
        [Tooltip("To START: how far left/right of your nose the ring may be.")]
        public float startSideways = 0.3f;
        [Tooltip("Once charging, the ring may drift this much further out of that box before the stance ends.")]
        public float stayMargin = 0.12f;

        [Tooltip("While in the stance you move at this fraction of your normal speed (dashing is off).")]
        [Range(0f, 1f)] public float moveSpeedFactor = 0.35f;

        private float nextBuzz;
        private float nextHint;

        private void Reset()
        {
            priority = 40;
        }

        private bool GripHeld()
        {
            return ctx.RingHand != null && ctx.RingHand.Grip >= gripThreshold;
        }

        // Ring position relative to the eyes: x = sideways, y = up, z = forward (flattened to where you face).
        private Vector3 RingOffset()
        {
            Transform head = ctx.Head.transform;
            Vector3 fwd = Gestures.FlatForward(ctx.Head);
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 d = ctx.RingHand.transform.position - head.position;
            return new Vector3(Vector3.Dot(d, right), d.y, Vector3.Dot(d, fwd));
        }

        private bool InFrontOfFace(float margin)
        {
            Vector3 o = RingOffset();
            return o.z >= startForward.x - margin && o.z <= startForward.y + margin
                && o.y >= startHeight.x - margin && o.y <= startHeight.y + margin
                && Mathf.Abs(o.x) <= startSideways + margin;
        }

        /// <summary>One line for the debug panel: grip pressure and where the ring is, so it's clear what's missing.</summary>
        public string StatusText()
        {
            if (ctx == null || ctx.RingHand == null || ctx.Head == null) return "pick ring hand first";
            Vector3 o = RingOffset();
            return "ring grip " + ctx.RingHand.Grip.ToString("0.0") + ", ring " + o.z.ToString("0.00") + " m ahead "
                   + o.y.ToString("+0.00;-0.00") + " up " + o.x.ToString("+0.00;-0.00") + " side"
                   + (IsActive ? "  CHARGING" : "  (fist + ring in front of your face)");
        }

        public override bool TryStart(int freeHands)
        {
            if (freeHands != BothMask || ctx.RingHand == null || ctx.Head == null) return false;
            if (!ctx.RingHand.IsTracked || !GripHeld()) return false;
            if (!InFrontOfFace(0f))
            {
                if (Time.time >= nextHint)
                {
                    nextHint = Time.time + 3f;
                    MoveLog.Add("Recharge: fist OK, bring the ring up in front of your face");
                }
                return false;
            }

            IsActive = true;
            UsedHands = BothMask;
            ctx.RingHand.Haptic(0.4f, 0.1f);
            MoveLog.Add("Recharging");
            return true;
        }

        public override void Tick()
        {
            string reason = null;
            if (!GripHeld()) reason = "fist released";
            else if (!ctx.RingHand.IsTracked || !InFrontOfFace(stayMargin)) reason = "ring lowered";
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
