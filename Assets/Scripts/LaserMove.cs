using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Main attack, ring hand only. Ring hand alone: every trigger pull fires an instant bolt.
    /// Focus stance (off hand resting on the ring hand): tap for a bolt, hold past a short delay for a continuous
    /// laser. The laser burns ring energy, hurts the first target it touches, and stops when you let go, stop
    /// pointing forward, break focus, swing the hand hard (that's a whirlwind), or run dry.
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
        [Tooltip("Hands between the focus distance and this (meters) are ambiguous (maybe a blast), so nothing fires.")]
        public float handsTogetherGap = 0.3f;
        [Tooltip("Beam bends onto a target within this many degrees. 0 turns aim assist off.")]
        public float aimAssistAngle = 7f;

        [Header("Focus (off hand on the ring hand)")]
        [Tooltip("Hands closer than this (meters) count as focusing: needed to fire the laser.")]
        public float focusDistance = 0.23f;
        [Tooltip("The laser stops if the hands drift further apart than this (meters).")]
        public float focusBreakDistance = 0.35f;

        private bool wasHeld = true;   // ignore a trigger that is already down when the ring hand is picked
        private bool waiting;          // focused pull: deciding between tap (bolt) and hold (laser)
        private bool ready;            // hold confirmed: start the laser
        private float pressTime;

        private TrackedNode hand;
        private float startTime;
        private bool loggedHit;
        private Transform beam;
        private Transform tip;

        private void Reset()
        {
            priority = 0;
        }

        /// <summary>Distance between the two hands, or a big number if either is missing.</summary>
        public float HandGap()
        {
            if (ctx == null || ctx.Left == null || ctx.Right == null) return 99f;
            return (ctx.Left.transform.position - ctx.Right.transform.position).magnitude;
        }

        public bool IsFocused()
        {
            return ctx != null && ctx.RingHand != null && ctx.OffHand != null && ctx.OffHand.IsTracked
                   && HandGap() < focusDistance;
        }

        public override void Observe()
        {
            if (ctx == null || ctx.RingHand == null) return;
            TrackedNode h = ctx.RingHand;
            bool held = h.TriggerHeld;

            if (held && !wasHeld)
            {
                ready = false;
                waiting = false;
                if (CheckPose(h))
                {
                    if (IsFocused())
                    {
                        // Focus stance: wait to see if this is a tap (bolt) or a hold (laser).
                        waiting = true;
                        pressTime = Time.time;
                    }
                    else
                    {
                        // Ring hand alone: every pull is an instant bolt.
                        FireBolt(h);
                    }
                }
            }

            if (waiting)
            {
                if (ctx.controller.IsHandBusy(h.HandMask))
                {
                    waiting = false;   // another move (blast) took the hand
                }
                else if (!held)
                {
                    waiting = false;
                    FireBolt(h);
                }
                else if (Time.time - pressTime >= holdDelay)
                {
                    waiting = false;
                    if (!IsFocused()) MoveLog.Add("Laser skipped: focus lost (hands " + HandGap().ToString("0.00") + " m apart)");
                    else if (h.Velocity.magnitude > startMaxHandSpeed) MoveLog.Add("Laser skipped: hand moving");
                    else ready = true;
                }
            }

            if (!held) ready = false;
            wasHeld = held;
        }

        public override bool TryStart(int freeHands)
        {
            // The laser uses both hands: the ring hand fires, the off hand focuses.
            if (!ready || (freeHands & BothMask) != BothMask) return false;
            ready = false;
            return Begin(ctx.RingHand);
        }

        // Checked once when the trigger is pulled. Returns true if this pull may fire; otherwise logs why not.
        private bool CheckPose(TrackedNode h)
        {
            string who = "Ring";
            if (!h.IsTracked) return false;
            if (ctx.controller.IsHandBusy(h.HandMask)) return false;
            TrackedNode off = ctx.OffHand;
            if (off != null && off.TriggerHeld) return false;   // both triggers: that's the charged blast
            if (h.Velocity.magnitude > startMaxHandSpeed) { MoveLog.Add(who + " skipped: hand moving"); return false; }
            float gap = HandGap();
            if (gap >= focusDistance && gap < handsTogetherGap)
            {
                MoveLog.Add(who + " skipped: hands half together (" + gap.ToString("0.00") + " m)");
                return false;
            }
            float fwd = Gestures.ForwardDot(ctx.Head, h);
            if (fwd < minForwardDot) { MoveLog.Add(who + " skipped: not pointing fwd (" + fwd.ToString("0.00") + ")"); return false; }
            if (!ctx.energy.CanAfford(Mathf.Min(boltCost, minEnergyToStart)))
            {
                MoveLog.Add(who + " skipped: no energy");
                h.Haptic(0.15f, 0.05f);
                return false;
            }
            return true;
        }

        private void FireBolt(TrackedNode h)
        {
            if (!ctx.energy.TrySpend(boltCost)) { MoveLog.Add("Bolt skipped: no energy"); return; }

            Vector3 dir = h.AimForward;
            Vector3 origin = h.AimOrigin + dir * 0.12f;
            bool assisted = false;
            if (aimAssistAngle > 0f) dir = AimAssist.Adjust(origin, dir, aimAssistAngle + 2f, out assisted);
            Projectile.Spawn(beamMaterial, origin, dir, boltSpeed, boltDamage, 0.07f, boltKnockback);
            h.Haptic(0.5f, 0.08f);
            MoveLog.Add("Bolt fired" + (assisted ? " (assist)" : ""));
        }

        private bool Begin(TrackedNode h)
        {
            hand = h;
            IsActive = true;
            UsedHands = BothMask;
            startTime = Time.time;
            loggedHit = false;

            GameObject b = Fx.Shape(PrimitiveType.Cylinder, beamMaterial, Vector3.one * beamWidth);
            b.name = "Laser Beam";
            beam = b.transform;
            GameObject t = Fx.Shape(PrimitiveType.Sphere, beamMaterial, Vector3.one * beamWidth * 4f);
            t.name = "Laser Tip";
            tip = t.transform;

            if (ctx.OffHand != null) ctx.OffHand.Haptic(0.4f, 0.1f);
            MoveLog.Add("Laser on (focused)");
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
            else if (HandGap() > focusBreakDistance) reason = "focus lost";
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
            IDamageable target = null;
            Vector3 hitPoint = end;
            string hitName = null;

            IDamageable shapeTarget;
            Vector3 shapePoint;
            float fraction;
            bool shapeHit = HitShapes.Segment(origin, end, beamWidth, out shapeTarget, out shapePoint, out fraction);
            float shapeDist = shapeHit ? fraction * range : float.MaxValue;

            Physics.SyncTransforms();
            RaycastHit hit;
            bool physHit = Physics.SphereCast(origin, beamWidth, dir, out hit, range, ~0, QueryTriggerInteraction.Ignore);
            float physDist = physHit ? hit.distance : float.MaxValue;

            if (shapeHit && shapeDist <= physDist)
            {
                target = shapeTarget;
                hitPoint = shapePoint;
                end = origin + dir * shapeDist;
                hitName = "target";
            }
            else if (physHit)
            {
                end = hit.point;
                hitPoint = hit.point;
                IDamageable t = hit.collider.GetComponentInParent<IDamageable>();
                if (t != null && t.IsAlive)
                {
                    target = t;
                    hitName = hit.collider.name;
                }
            }

            if (target != null)
            {
                target.TakeDamage(damagePerSecond * Time.deltaTime, hitPoint, dir * (knockbackPerSecond * Time.deltaTime));
                if (!loggedHit)
                {
                    loggedHit = true;
                    MoveLog.Add("Laser HIT " + hitName);
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
