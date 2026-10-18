using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Basic attack: point at the target and pull the trigger. The bolt fires the moment the trigger is pulled.
    /// It's skipped when the pull is clearly meant for another move:
    ///   - the hand is in the shield pose (controller pointing up or sideways),
    ///   - the hand is swinging fast (starting a whirlwind),
    ///   - both hands are held together (charging a blast).
    /// Each skip is written to the debug readout with the reason.
    /// </summary>
    public class EnergyBoltMove : Move
    {
        public Material boltMaterial;
        public float cost = 6f;
        public float damage = 10f;
        public float speed = 22f;
        public float radius = 0.07f;
        public float knockback = 2.5f;
        [Tooltip("Minimum seconds between shots from the same hand.")]
        public float cooldown = 0.2f;
        [Tooltip("Hand must be moving slower than this (m/s) to fire, so swinging into a whirlwind doesn't shoot.")]
        public float maxHandSpeed = 1.6f;
        [Tooltip("The controller must point at least this much forward (1 = straight ahead). Lower = more forgiving.")]
        public float minForwardDot = 0.5f;
        [Tooltip("Hands closer together than this (meters) count as charging a blast, so no bolt.")]
        public float handsTogetherGap = 0.3f;
        [Tooltip("Shots aimed within this many degrees of a target curve onto it. 0 turns aim assist off.")]
        public float aimAssistAngle = 9f;

        private readonly bool[] wasHeld = new bool[2];
        private readonly float[] lastShot = { -10f, -10f };

        private void Reset()
        {
            priority = 0;
        }

        public override void Observe()
        {
            if (ctx == null) return;
            Track(0, ctx.Left, LeftMask);
            Track(1, ctx.Right, RightMask);
        }

        private void Track(int i, TrackedNode hand, int mask)
        {
            if (hand == null) return;
            bool held = hand.TriggerHeld;
            if (held && !wasHeld[i])
                TryFire(i, hand, mask);
            wasHeld[i] = held;
        }

        private void TryFire(int i, TrackedNode hand, int mask)
        {
            string who = "Bolt " + MoveLog.HandName(hand);
            if (!hand.IsTracked) return;
            if (ctx.controller.IsHandBusy(mask)) return;   // another move is using this hand

            if (Time.time - lastShot[i] < cooldown) return;
            if (hand.Velocity.magnitude > maxHandSpeed) { MoveLog.Add(who + " skipped: hand moving"); return; }
            if ((ctx.Left.transform.position - ctx.Right.transform.position).magnitude < handsTogetherGap)
            {
                MoveLog.Add(who + " skipped: hands together");
                return;
            }
            float fwd = Gestures.ForwardDot(ctx.Head, hand);
            if (fwd < minForwardDot) { MoveLog.Add(who + " skipped: not pointing fwd (" + fwd.ToString("0.00") + ")"); return; }
            if (!ctx.energy.TrySpend(cost))
            {
                MoveLog.Add(who + " skipped: no energy");
                hand.Haptic(0.15f, 0.05f);
                return;
            }

            Vector3 dir = hand.AimForward;
            Vector3 origin = hand.AimOrigin + dir * 0.12f;
            bool assisted = false;
            if (aimAssistAngle > 0f)
                dir = AimAssist.Adjust(origin, dir, aimAssistAngle, out assisted);

            Projectile.Spawn(boltMaterial, origin, dir, speed, damage, radius, knockback);
            lastShot[i] = Time.time;
            hand.Haptic(0.5f, 0.08f);
            MoveLog.Add(who + " fired" + (assisted ? " (assist)" : ""));
        }

        // The bolt fires from Observe (on trigger pull), so it never claims a hand.
        public override bool TryStart(int freeHands)
        {
            return false;
        }
    }
}
