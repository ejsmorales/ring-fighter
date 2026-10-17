using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Basic attack: quickly tap the trigger and let go to fire a bolt where the controller points.
    /// It fires on release so a trigger press that turns into a shield or whirlwind doesn't also shoot.
    /// </summary>
    public class EnergyBoltMove : Move
    {
        public Material boltMaterial;
        public float cost = 8f;
        public float damage = 10f;
        public float speed = 20f;
        public float radius = 0.07f;
        public float knockback = 2.5f;
        [Tooltip("Longest a trigger press can last and still count as a tap.")]
        public float tapWindow = 0.28f;

        private readonly float[] pressTime = new float[2];
        private readonly bool[] wasHeld = new bool[2];
        private readonly int[] claimsAtPress = new int[2];

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
            {
                pressTime[i] = Time.time;
                claimsAtPress[i] = ctx.controller.GetHandClaims(mask);
            }
            else if (!held && wasHeld[i])
            {
                bool quick = Time.time - pressTime[i] <= tapWindow;
                bool untouched = ctx.controller.GetHandClaims(mask) == claimsAtPress[i];
                if (quick && untouched)
                    Fire(hand);
            }
            wasHeld[i] = held;
        }

        private void Fire(TrackedNode hand)
        {
            if (!ctx.energy.TrySpend(cost))
            {
                hand.Haptic(0.15f, 0.05f);
                return;
            }
            Vector3 dir = hand.AimForward;
            Vector3 origin = hand.AimOrigin + dir * 0.12f;
            Projectile.Spawn(boltMaterial, origin, dir, speed, damage, radius, knockback);
            hand.Haptic(0.5f, 0.08f);
        }

        // The bolt is fired from Observe (on trigger release), so it never claims a hand for a lasting move.
        public override bool TryStart(int freeHands)
        {
            return false;
        }
    }
}
