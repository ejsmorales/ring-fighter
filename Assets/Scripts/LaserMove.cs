using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Basic attack: point forward and hold the trigger to fire a continuous energy laser. It burns ring
    /// energy while it's on, hurts the first target it touches, and stops when you let go, stop pointing
    /// forward, swing the hand hard (that's a whirlwind), or run dry. A quick tap gives a short flash of beam.
    /// Replaces the old single energy bolt (EnergyBoltMove is still in the project but no longer used).
    /// </summary>
    public class LaserMove : Move
    {
        public Material beamMaterial;
        [Tooltip("Energy burned per second while firing.")]
        public float drainPerSecond = 20f;
        [Tooltip("Energy needed to start firing.")]
        public float minEnergyToStart = 8f;
        public float damagePerSecond = 36f;
        public float range = 25f;
        public float beamWidth = 0.025f;
        public float knockbackPerSecond = 2f;
        [Tooltip("Even a quick tap keeps the beam on this long (seconds).")]
        public float minBurstTime = 0.12f;

        [Header("Pose rules")]
        [Tooltip("Controller must point at least this much forward to start (1 = straight ahead).")]
        public float minForwardDot = 0.5f;
        [Tooltip("Beam stops if the controller points less forward than this.")]
        public float stayForwardDot = 0.25f;
        [Tooltip("Hand must be slower than this (m/s) when the trigger is pulled, so swinging into a whirlwind doesn't fire.")]
        public float startMaxHandSpeed = 1.6f;
        [Tooltip("Beam cuts out if the hand moves faster than this (m/s), freeing it for a whirlwind.")]
        public float breakHandSpeed = 2.4f;
        [Tooltip("Hands closer than this (meters) are charging a blast, so no laser.")]
        public float handsTogetherGap = 0.3f;
        [Tooltip("Beam bends onto a target within this many degrees. 0 turns aim assist off.")]
        public float aimAssistAngle = 7f;

        private readonly bool[] wasHeld = new bool[2];
        private readonly bool[] pressed = new bool[2];   // trigger was just pulled and not yet used

        private TrackedNode hand;
        private float startTime;
        private bool loggedHit;
        private Transform beam;
        private Transform tip;

        private void Reset()
        {
            priority = 0;
        }

        public override void Observe()
        {
            if (ctx == null) return;
            Track(0, ctx.Left);
            Track(1, ctx.Right);
            // A pull that another move (shield, whirlwind, blast) owns must not fire the laser later.
            if (ctx.controller.IsHandBusy(LeftMask) && !(IsActive && UsedHands == LeftMask)) pressed[0] = false;
            if (ctx.controller.IsHandBusy(RightMask) && !(IsActive && UsedHands == RightMask)) pressed[1] = false;
        }

        private void Track(int i, TrackedNode h)
        {
            if (h == null) return;
            bool held = h.TriggerHeld;
            if (held && !wasHeld[i]) pressed[i] = true;
            if (!held) pressed[i] = false;
            wasHeld[i] = held;
        }

        public override bool TryStart(int freeHands)
        {
            if ((freeHands & LeftMask) != 0 && pressed[0] && Evaluate(0, ctx.Left)) return Begin(ctx.Left);
            if ((freeHands & RightMask) != 0 && pressed[1] && Evaluate(1, ctx.Right)) return Begin(ctx.Right);
            return false;
        }

        // Called once per trigger pull. Returns true if the laser should fire; otherwise logs why not.
        private bool Evaluate(int i, TrackedNode h)
        {
            pressed[i] = false;   // one decision per pull
            string who = "Laser " + MoveLog.HandName(h);
            if (!h.IsTracked) return false;
            if (h.Velocity.magnitude > startMaxHandSpeed) { MoveLog.Add(who + " skipped: hand moving"); return false; }
            if ((ctx.Left.transform.position - ctx.Right.transform.position).magnitude < handsTogetherGap)
            {
                MoveLog.Add(who + " skipped: hands together");
                return false;
            }
            float fwd = Gestures.ForwardDot(ctx.Head, h);
            if (fwd < minForwardDot) { MoveLog.Add(who + " skipped: not pointing fwd (" + fwd.ToString("0.00") + ")"); return false; }
            if (!ctx.energy.CanAfford(minEnergyToStart))
            {
                MoveLog.Add(who + " skipped: no energy");
                h.Haptic(0.15f, 0.05f);
                return false;
            }
            return true;
        }

        private bool Begin(TrackedNode h)
        {
            hand = h;
            IsActive = true;
            UsedHands = h.HandMask;
            startTime = Time.time;
            loggedHit = false;

            GameObject b = Fx.Shape(PrimitiveType.Cylinder, beamMaterial, Vector3.one * beamWidth);
            b.name = "Laser Beam";
            beam = b.transform;
            GameObject t = Fx.Shape(PrimitiveType.Sphere, beamMaterial, Vector3.one * beamWidth * 4f);
            t.name = "Laser Tip";
            tip = t.transform;

            MoveLog.Add("Laser " + MoveLog.HandName(h) + " on");
            return true;
        }

        public override void Tick()
        {
            if (hand == null || beam == null)
            {
                Finish();
                return;
            }

            bool minTimeDone = Time.time - startTime >= minBurstTime;
            string reason = null;
            if (!hand.TriggerHeld && minTimeDone) reason = "trigger released";
            else if (hand.Velocity.magnitude > breakHandSpeed) reason = "hand swinging";
            else if (Gestures.ForwardDot(ctx.Head, hand) < stayForwardDot) reason = "stopped pointing fwd";
            else if (!ctx.energy.Drain(drainPerSecond)) reason = "out of energy";
            if (reason != null)
            {
                MoveLog.Add("Laser ended: " + reason);
                Finish();
                return;
            }

            Vector3 dir = hand.AimForward;
            Vector3 origin = hand.AimOrigin + dir * 0.1f;
            if (aimAssistAngle > 0f)
            {
                bool assisted;
                dir = AimAssist.Adjust(origin, dir, aimAssistAngle, out assisted);
            }

            Vector3 end = origin + dir * range;
            Physics.SyncTransforms();
            RaycastHit hit;
            if (Physics.SphereCast(origin, beamWidth, dir, out hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(damagePerSecond * Time.deltaTime, hit.point, dir * (knockbackPerSecond * Time.deltaTime));
                    if (!loggedHit)
                    {
                        loggedHit = true;
                        MoveLog.Add("Laser HIT " + hit.collider.name);
                    }
                }
            }

            Vector3 delta = end - origin;
            float length = delta.magnitude;
            beam.position = origin + delta * 0.5f;
            if (length > 0.001f) beam.rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
            beam.localScale = new Vector3(beamWidth, length * 0.5f, beamWidth);
            tip.position = end;

            hand.Haptic(0.3f, 0.03f);
        }

        protected override void OnFinish()
        {
            if (beam != null) Destroy(beam.gameObject);
            if (tip != null) Destroy(tip.gameObject);
            beam = null;
            tip = null;
            hand = null;
        }
    }
}
