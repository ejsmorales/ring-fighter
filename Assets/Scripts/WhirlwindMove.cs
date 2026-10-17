using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Area attack: hold the trigger and swing your arm in circles to spin up a whirlwind that
    /// drifts forward and shreds anything inside it. Keep circling to keep it alive; it drains energy.
    /// </summary>
    public class WhirlwindMove : Move
    {
        public Material whirlwindMaterial;
        public float drainPerSecond = 20f;
        public float minEnergyToStart = 14f;
        public float damagePerSecond = 28f;
        public float radius = 1.4f;
        public float travelSpeed = 2.2f;
        public float maxDuration = 6f;
        [Tooltip("How far around a circle (degrees) the hand must go to start the whirlwind.")]
        public float startAngle = 320f;
        [Tooltip("Whirlwind keeps going while the hand has swept at least this many degrees recently.")]
        public float sustainAngle = 200f;
        [Tooltip("Seconds the whirlwind survives after circling stops.")]
        public float gracePeriod = 0.45f;

        private TrackedNode hand;
        private GameObject root;
        private Transform[] discs;
        private Vector3 direction;
        private float startTime;
        private float lastCircleTime;

        private readonly Collider[] overlap = new Collider[24];
        private readonly System.Collections.Generic.List<IDamageable> hitThisFrame = new System.Collections.Generic.List<IDamageable>();

        private void Reset()
        {
            priority = 20;
        }

        public override bool TryStart(int freeHands)
        {
            if ((freeHands & LeftMask) != 0 && CanStart(ctx.Left)) return Begin(ctx.Left);
            if ((freeHands & RightMask) != 0 && CanStart(ctx.Right)) return Begin(ctx.Right);
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
            lastCircleTime = Time.time;
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
            return true;
        }

        public override void Tick()
        {
            if (hand == null || root == null)
            {
                Finish();
                return;
            }

            float angle;
            if (Gestures.IsCircling(hand, 0.85f, sustainAngle, out angle))
                lastCircleTime = Time.time;

            bool stillCircling = Time.time - lastCircleTime <= gracePeriod;
            bool timeLeft = Time.time - startTime <= maxDuration;
            if (!hand.TriggerHeld || !stillCircling || !timeLeft || !ctx.energy.Drain(drainPerSecond))
            {
                Finish();
                return;
            }

            root.transform.position += direction * (travelSpeed * Time.deltaTime);
            for (int i = 0; i < discs.Length; i++)
                discs[i].Rotate(0f, (360f + 220f * i) * Time.deltaTime, 0f, Space.Self);

            hand.Haptic(0.25f, 0.03f);
            DamageInside();
        }

        private void DamageInside()
        {
            Vector3 center = root.transform.position + Vector3.up * 1.0f;
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlap, ~0, QueryTriggerInteraction.Ignore);
            hitThisFrame.Clear();
            for (int i = 0; i < count; i++)
            {
                IDamageable target = overlap[i].GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || hitThisFrame.Contains(target)) continue;
                hitThisFrame.Add(target);

                Vector3 point = overlap[i].ClosestPoint(center);
                Vector3 push = (point - center);
                push.y = 0f;
                push = push.sqrMagnitude > 0.0001f ? push.normalized * 2f : direction;
                target.TakeDamage(damagePerSecond * Time.deltaTime, point, push);
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
