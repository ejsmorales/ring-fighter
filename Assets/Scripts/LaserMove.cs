using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Basic attack: point forward and tap the trigger for a single energy bolt, or keep holding it past a short delay for a
    /// continuous laser. The laser burns ring energy while it's on, hurts the first target it touches, and stops
    /// when you let go, stop pointing forward, swing the hand hard (that's a whirlwind), or run dry.
    /// Replaces EnergyBoltMove, which is still in the project but no longer used.
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
        [Tooltip("Trigger must stay held this long (seconds) before the laser starts. Releasing sooner fires a single bolt.")]
        public float holdDelay = 0.22f;
        [Tooltip("Once the laser is on, it stays on at least this long (seconds).")]
        public float minBurstTime = 0.12f;

        [Header("Single bolt (quick tap)")]
        public float boltCost = 6f;
        public float boltDamage = 10f;
        public float boltSpeed = 22f;
        public float boltKnockback = 2.5f;

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
        private readonly bool[] waiting = new bool[2];    // trigger pulled, still deciding tap or hold
        private readonly bool[] armed = new bool[2];      // pose was good when the trigger was pulled
        private readonly bool[] ready = new bool[2];      // hold confirmed: start the laser
        private readonly float[] pressTime = new float[2];

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
            Track(0, ctx.Left, LeftMask);
            Track(1, ctx.Right, RightMask);
        }

        private void Track(int i, TrackedNode h, int mask)
        {
            if (h == null) return;
            bool held = h.TriggerHeld;

            if (held && !wasHeld[i])
            {
                waiting[i] = true;
                ready[i] = false;
                pressTime[i] = Time.time;
                armed[i] = CheckPose(h);
            }

            if (waiting[i])
            {
                if (ctx.controller.IsHandBusy(mask))
                {
                    waiting[i] = false;   // another move owns this hand
                }
                else if (!held)
                {
                    // Released quickly: that was a tap, so fire a single bolt.
                    waiting[i] = false;
                    if (armed[i]) FireBolt(h);
                }
                else if (Time.time - pressTime[i] >= holdDelay)
                {
                    // Still held after the delay: that's a hold, so start the laser.
                    waiting[i] = false;
                    if (armed[i] && h.Velocity.magnitude <= startMaxHandSpeed) ready[i] = true;
                    else if (armed[i]) MoveLog.Add("Laser " + MoveLog.HandName(h) + " skipped: hand moving");
                }
            }

            if (!held) ready[i] = false;
            wasHeld[i] = held;
        }

        public override bool TryStart(int freeHands)
        {
            if ((freeHands & LeftMask) != 0 && ready[0]) { ready[0] = false; return Begin(ctx.Left); }
            if ((freeHands & RightMask) != 0 && ready[1]) { ready[1] = false; return Begin(ctx.Right); }
            return false;
        }

        // Checked once when the trigger is pulled. Returns true if this pull may fire; otherwise logs why not.
        private bool CheckPose(TrackedNode h)
        {
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

        private void FireBolt(TrackedNode h)
        {
            string who = "Bolt " + MoveLog.HandName(h);
            if (!ctx.energy.TrySpend(boltCost)) { MoveLog.Add(who + " skipped: no energy"); return; }

            Vector3 dir = h.AimForward;
            Vector3 origin = h.AimOrigin + dir * 0.12f;
            bool assisted = false;
            if (aimAssistAngle > 0f) dir = AimAssist.Adjust(origin, dir, aimAssistAngle + 2f, out assisted);
            Projectile.Spawn(beamMaterial, origin, dir, boltSpeed, boltDamage, 0.07f, boltKnockback);
            h.Haptic(0.5f, 0.08f);
            MoveLog.Add(who + " fired" + (assisted ? " (assist)" : ""));
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
