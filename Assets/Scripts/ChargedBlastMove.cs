using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Heavy attack: hold both triggers with both hands together off to one side to charge an energy ball.
    /// Let go of either trigger to fire it. Charging drains energy, and a longer charge hits harder.
    /// </summary>
    public class ChargedBlastMove : Move
    {
        public Material blastMaterial;
        public float chargeTime = 2f;
        public float minChargeToFire = 0.25f;
        public float drainPerSecond = 22f;
        public float minEnergyToStart = 12f;
        public float minDamage = 20f;
        public float maxDamage = 90f;
        public float speed = 14f;
        public float minRadius = 0.10f;
        public float maxRadius = 0.32f;
        public float knockback = 6f;
        [Tooltip("How close together the hands must be to start charging.")]
        public float startMaxHandGap = 0.30f;
        [Tooltip("Hands pulling further apart than this cancels the charge.")]
        public float cancelHandGap = 0.60f;
        [Tooltip("How far to one side of the head the hands must be to start charging.")]
        public float minSideOffset = 0.18f;
        [Tooltip("Aim where you look (true) or where the controllers point (false).")]
        public bool aimWithHead = true;
        [Tooltip("Blasts aimed within this many degrees of a target curve onto it. 0 turns aim assist off.")]
        public float aimAssistAngle = 14f;

        private float charge;      // 0 to 1
        private GameObject orb;

        /// <summary>The placeholder orb while charging, null otherwise (read by BlastChargeVfx).</summary>
        public Transform OrbPlaceholder { get { return orb != null ? orb.transform : null; } }

        /// <summary>Current charge, 0 to 1 (read by BlastChargeVfx).</summary>
        public float Charge01 { get { return charge; } }

        private void Reset()
        {
            priority = 30;
        }

        public override bool TryStart(int freeHands)
        {
            if (freeHands != BothMask) return false;
            TrackedNode l = ctx.Left, r = ctx.Right;
            if (l == null || r == null || !l.IsTracked || !r.IsTracked) return false;
            if (!l.TriggerHeld || !r.TriggerHeld) return false;
            if (!Gestures.HandsTogetherAtSide(ctx.Head, l, r, startMaxHandGap, minSideOffset)) return false;
            if (!ctx.energy.CanAfford(minEnergyToStart)) return false;

            IsActive = true;
            UsedHands = BothMask;
            charge = 0f;
            MoveLog.Add("Blast charging");
            orb = Fx.Shape(PrimitiveType.Sphere, blastMaterial, Vector3.one * minRadius * 2f);
            orb.name = "Charging Blast";
            PlaceOrb();
            return true;
        }

        public override void Tick()
        {
            TrackedNode l = ctx.Left, r = ctx.Right;
            float gap = (l.transform.position - r.transform.position).magnitude;
            bool released = !l.TriggerHeld || !r.TriggerHeld;

            if (gap > cancelHandGap)
            {
                MoveLog.Add("Blast fizzled: hands apart");
                Finish();   // pulled the hands apart: fizzle, no shot
                return;
            }

            bool hasEnergy = ctx.energy.Drain(drainPerSecond);
            charge = Mathf.Clamp01(charge + Time.deltaTime / chargeTime);

            float buzz = 0.1f + 0.5f * charge;
            l.Haptic(buzz, 0.03f);
            r.Haptic(buzz, 0.03f);

            // A full charge simply holds (charge is clamped) until a trigger is released.
            if (released || !hasEnergy)
            {
                FireAndFinish();
                return;
            }
            PlaceOrb();
        }

        private void PlaceOrb()
        {
            if (orb == null) return;
            Vector3 mid = (ctx.Left.transform.position + ctx.Right.transform.position) * 0.5f;
            Vector3 dir = AimDirection();
            orb.transform.position = mid + dir * 0.18f;
            orb.transform.localScale = Vector3.one * Mathf.Lerp(minRadius, maxRadius, charge) * 2f;
        }

        private Vector3 AimDirection()
        {
            if (aimWithHead && ctx.Head != null)
                return ctx.Head.transform.forward;
            Vector3 d = ctx.Left.AimForward + ctx.Right.AimForward;
            if (d.sqrMagnitude < 0.01f) d = ctx.rig.FlatForward();
            return d.normalized;
        }

        private void FireAndFinish()
        {
            if (charge >= minChargeToFire)
            {
                float radius = Mathf.Lerp(minRadius, maxRadius, charge);
                Vector3 origin = orb != null ? orb.transform.position : (ctx.Left.transform.position + ctx.Right.transform.position) * 0.5f;
                bool assisted = false;
                Vector3 dir = AimDirection();
                if (aimAssistAngle > 0f)
                    dir = AimAssist.Adjust(origin, dir, aimAssistAngle, out assisted);
                Projectile.Spawn(blastMaterial, origin, dir, speed, Mathf.Lerp(minDamage, maxDamage, charge), radius, knockback, true);
                ctx.Left.Haptic(0.9f, 0.15f);
                ctx.Right.Haptic(0.9f, 0.15f);
                MoveLog.Add("Blast fired " + Mathf.RoundToInt(charge * 100f) + "%" + (assisted ? " (assist)" : ""));
            }
            else
            {
                MoveLog.Add("Blast fizzled: released too early");
            }
            Finish();
        }

        protected override void OnFinish()
        {
            if (orb != null) Destroy(orb);
            orb = null;
            charge = 0f;
        }
    }
}
