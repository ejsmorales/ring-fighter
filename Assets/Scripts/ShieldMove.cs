using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Defense: raise your forearm in front of you to block (controller pointing up or sideways, not at the
    /// opponent) and hold the trigger. A barrier appears in front of that hand, facing forward. Drains energy
    /// while held. Opponents (later) can call Blocks() to see if a shot is stopped.
    /// </summary>
    public class ShieldMove : Move
    {
        public Material shieldMaterial;
        public float drainPerSecond = 8f;
        public float minEnergyToStart = 5f;
        public float shieldRadius = 0.42f;
        [Tooltip("How far in front of the hand the barrier sits (meters).")]
        public float distanceFromHand = 0.18f;
        [Tooltip("To START a shield the controller must point forward less than this (1 = straight ahead). Lower = stricter.")]
        public float startMaxForwardDot = 0.45f;
        [Tooltip("The shield STAYS up until the controller points forward more than this. Higher than the start value so it doesn't flicker.")]
        public float stayMaxForwardDot = 0.7f;
        [Tooltip("Hand must be steadier than this (m/s) to start a shield, so swinging into a whirlwind doesn't start one.")]
        public float startMaxSpeed = 1.5f;
        [Tooltip("Trigger must be held this long in the blocking pose before the shield appears.")]
        public float holdDelay = 0.1f;

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
                && Gestures.IsGuarding(ctx.Head, h, startMaxForwardDot)
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
            MoveLog.Add("Shield " + MoveLog.HandName(h) + " up");
            return true;
        }

        public override void Tick()
        {
            string reason = null;
            if (hand == null || !hand.IsTracked) reason = "lost tracking";
            else if (!hand.TriggerHeld) reason = "trigger released";
            else if (!Gestures.IsGuarding(ctx.Head, hand, stayMaxForwardDot)) reason = "left blocking pose";
            else if (!ctx.energy.Drain(drainPerSecond)) reason = "out of energy";

            if (reason != null)
            {
                MoveLog.Add("Shield " + MoveLog.HandName(hand) + " down: " + reason);
                Finish();
                return;
            }
            PlaceDisc();
        }

        private void PlaceDisc()
        {
            if (disc == null || hand == null) return;
            // Face the barrier the way the player is facing, so it covers them no matter how the wrist is turned.
            Vector3 forward = Gestures.FlatForward(ctx.Head);
            disc.transform.position = hand.transform.position + forward * distanceFromHand;
            // Cylinders stand along their local Y axis, so tip it to face forward.
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
