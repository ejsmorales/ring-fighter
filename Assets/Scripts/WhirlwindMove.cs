using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Area attack (ring hand): hold the trigger and swing your arm in circles to spin up a whirlwind. Once it
    /// starts you can rest your arm: it keeps travelling for as long as the trigger stays held, getting faster
    /// the longer it runs, and gently follows where you look. It shreds anything inside it, lifts a target into
    /// the air (it takes fall damage on landing) and slows that target's attacks. Drains energy; release to end it.
    /// </summary>
    public class WhirlwindMove : Move
    {
        public Material whirlwindMaterial;
        public float drainPerSecond = 14f;
        public float minEnergyToStart = 14f;
        public float damagePerSecond = 28f;
        public float radius = 1.4f;
        [Tooltip("Speed (m/s) when it starts.")]
        public float travelSpeed = 2.2f;
        [Tooltip("Top speed (m/s) after it has run for 'Ramp Time'.")]
        public float maxTravelSpeed = 5.5f;
        public float rampTime = 3f;
        public float maxDuration = 8f;
        [Tooltip("It fades out after travelling this far (meters).")]
        public float maxDistance = 14f;
        [Tooltip("How fast it turns to follow where you look (degrees per second).")]
        public float steerDegreesPerSecond = 60f;
        [Tooltip("How far around a circle (degrees) the hand must go to start the whirlwind.")]
        public float startAngle = 320f;

        private TrackedNode hand;
        private GameObject root;
        private Transform[] discs;
        private Vector3 direction;
        private float startTime;
        private float travelled;

        private readonly Collider[] overlap = new Collider[24];
        private readonly System.Collections.Generic.List<IDamageable> hitThisFrame = new System.Collections.Generic.List<IDamageable>();
        private readonly System.Collections.Generic.List<Vector3> hitPoints = new System.Collections.Generic.List<Vector3>();
        private bool loggedHit;

        private void Reset()
        {
            priority = 20;
        }

        public override bool TryStart(int freeHands)
        {
            // Ring hand only: the off hand is for support.
            TrackedNode r = ctx.RingHand;
            if (r != null && (freeHands & r.HandMask) != 0 && CanStart(r)) return Begin(r);
            return false;
        }

        private bool CanStart(TrackedNode h)
        {
            float angle;
            return h != null && h.IsTracked && h.TriggerHeld
                && ctx.energy.CanAfford(minEnergyToStart)
                && Gestures.IsCircling(h, 0.85f, startAngle, out angle);
        }

        private bool Begin(TrackedNode h)
        {
            hand = h;
            IsActive = true;
            UsedHands = h.HandMask;
            startTime = Time.time;
            travelled = 0f;
            loggedHit = false;
            direction = ctx.rig.FlatForward();

            root = new GameObject("Whirlwind");
            root.transform.position = GroundPoint(ctx.Head.transform.position) + direction * 1.0f;

            // Three flattened discs of different sizes, spinning at different speeds, read as a vortex.
            discs = new Transform[3];
            for (int i = 0; i < discs.Length; i++)
            {
                float size = radius * 2f * (0.55f + 0.225f * i);
                GameObject d = Fx.Shape(PrimitiveType.Cylinder, whirlwindMaterial, new Vector3(size, 0.015f, size), root.transform);
                d.name = "Disc " + i;
                d.transform.localPosition = new Vector3(0f, 0.35f + 0.55f * i, 0f);
                discs[i] = d.transform;
            }

            hand.Haptic(0.6f, 0.12f);
            MoveLog.Add("Whirlwind " + MoveLog.HandName(h) + " started");
            return true;
        }

        public override void Tick()
        {
            if (hand == null || root == null)
            {
                Finish();
                return;
            }

            string reason = null;
            if (!hand.TriggerHeld) reason = "trigger released";
            else if (Time.time - startTime > maxDuration) reason = "time up";
            else if (travelled > maxDistance) reason = "reached max range";
            else if (!ctx.energy.Drain(drainPerSecond)) reason = "out of energy";
            if (reason != null)
            {
                MoveLog.Add("Whirlwind ended: " + reason);
                Finish();
                return;
            }

            // Gently follow the player's gaze, and speed up the longer it runs.
            direction = Vector3.RotateTowards(direction, ctx.rig.FlatForward(), steerDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime, 0f);
            float ramp = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(0.01f, rampTime));
            float speed = Mathf.Lerp(travelSpeed, maxTravelSpeed, ramp);
            root.transform.position += direction * (speed * Time.deltaTime);
            travelled += speed * Time.deltaTime;
            for (int i = 0; i < discs.Length; i++)
                discs[i].Rotate(0f, (360f + 220f * i) * Time.deltaTime, 0f, Space.Self);

            hand.Haptic(0.25f, 0.03f);
            DamageInside();
        }

        private void DamageInside()
        {
            Vector3 center = root.transform.position + Vector3.up * 1.0f;
            hitThisFrame.Clear();
            hitPoints.Clear();

            // Targets by plain math first (works even if colliders are missing in a build).
            HitShapes.Overlap(center, radius, hitThisFrame, hitPoints);
            for (int i = 0; i < hitThisFrame.Count; i++)
                Hurt(hitThisFrame[i], hitPoints[i], center);

            int count = Physics.OverlapSphereNonAlloc(center, radius, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                IDamageable target = overlap[i].GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || hitThisFrame.Contains(target)) continue;
                hitThisFrame.Add(target);

                Hurt(target, overlap[i].ClosestPoint(center), center);
            }
        }

        private void Hurt(IDamageable target, Vector3 point, Vector3 center)
        {
            Vector3 push = point - center;
            push.y = 0f;
            push = push.sqrMagnitude > 0.0001f ? push.normalized * 0.6f : direction * 0.6f;
            target.TakeDamage(damagePerSecond * Time.deltaTime, point, push, DamageKind.Area);
            target.OnWhirlwind(Time.deltaTime);
            if (!loggedHit)
            {
                loggedHit = true;
                MoveLog.Add("Whirlwind HIT");
            }
        }

        private static Vector3 GroundPoint(Vector3 p)
        {
            p.y = 0f;
            return p;
        }

        protected override void OnFinish()
        {
            if (root != null) Destroy(root);
            root = null;
            discs = null;
            hand = null;
        }
    }
}
