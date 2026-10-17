using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Defense: hold the trigger with your arm raised in front of you to project a barrier.
    /// Drains energy while held. Opponents (later) can ask IsBlocking / Blocks() to see if a shot is stopped.
    /// </summary>
    public class ShieldMove : Move
    {
        public Material shieldMaterial;
        public float drainPerSecond = 9f;
        public float minEnergyToStart = 6f;
        public float shieldRadius = 0.38f;
        public float distanceFromHand = 0.28f;
        [Tooltip("Hand must be steadier than this to start a shield (so circling doesn't start one).")]
        public float startMaxSpeed = 0.9f;
        [Tooltip("Shield drops if the hand moves faster than this.")]
        public float dropSpeed = 1.8f;
        [Tooltip("Trigger must be held this long in the guard pose before the shield appears. Keep it longer than the bolt's tap window so quick taps still shoot.")]
        public float holdDelay = 0.3f;

        private TrackedNode hand;
        private GameObject disc;
        private readonly float[] pressTime = new float[2];
        private readonly bool[] wasHeld = new bool[2];

        public bool IsBlocking { get { return IsActive && disc != null; } }

        /// <summary>True if a world-space point is inside the shield disc (for future enemy attacks).</summary>
        public bool Blocks(Vector3 worldPoint)
        {
            if (!IsBlocking) return false;
            Vector3 local = disc.transform.InverseTransformPoint(worldPoint);
            // The disc is a flattened cylinder; local XZ is its face, local Y its thickness.
            return new Vector2(local.x, local.z).magnitude <= 0.5f && Mathf.Abs(local.y) <= 1.5f;
        }

        private void Reset()
        {
            priority = 10;
        }

        public override void Observe()
        {
            if (ctx == null) return;
            TrackPress(0, ctx.Left);
            TrackPress(1, ctx.Right);
        }

        private void TrackPress(int i, TrackedNode h)
        {
            if (h == null) return;
            bool held = h.TriggerHeld;
            if (held && !wasHeld[i]) pressTime[i] = Time.time;
            wasHeld[i] = held;
        }

        public override bool TryStart(int freeHands)
        {
            if ((freeHands & LeftMask) != 0 && CanStart(ctx.Left, 0)) return Begin(ctx.Left);
            if ((freeHands & RightMask) != 0 && CanStart(ctx.Right, 1)) return Begin(ctx.Right);
            return false;
        }

        private bool CanStart(TrackedNode h, int index)
        {
            return h != null && h.IsTracked && h.TriggerHeld
                && Time.time - pressTime[index] >= holdDelay
                && Gestures.IsGuarding(ctx.Head, h)
                && h.Velocity.magnitude < startMaxSpeed
                && ctx.energy.CanAfford(minEnergyToStart);
        }

        private bool Begin(TrackedNode h)
        {
            hand = h;
            IsActive = true;
            UsedHands = h.HandMask;
            disc = Fx.Shape(PrimitiveType.Cylinder, shieldMaterial, new Vector3(shieldRadius * 2f, 0.01f, shieldRadius * 2f));
            disc.name = "Energy Shield";
            PlaceDisc();
            hand.Haptic(0.3f, 0.1f);
            return true;
        }

        public override void Tick()
        {
            if (hand == null || !hand.TriggerHeld || !hand.IsTracked
                || !Gestures.IsGuarding(ctx.Head, hand)
                || hand.Velocity.magnitude > dropSpeed
                || !ctx.energy.Drain(drainPerSecond))
            {
                Finish();
                return;
            }
            PlaceDisc();
        }

        private void PlaceDisc()
        {
            if (disc == null || hand == null) return;
            Vector3 forward = hand.AimForward;
            disc.transform.position = hand.AimOrigin + forward * distanceFromHand;
            // Cylinders stand along their local Y axis, so tip it to face along the aim direction.
            disc.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90f, 0f, 0f);
        }

        protected override void OnFinish()
        {
            if (disc != null) Destroy(disc);
            disc = null;
            hand = null;
        }
    }
}
