using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Defense, either hand: raise a forearm in front of you to block (controller pointing up or sideways, not at the
    /// opponent) and hold the trigger. A barrier appears in front of that hand, facing forward. Raise the OTHER arm
    /// into the same pose too and the two merge into one big, stronger shield between your hands. Drains energy
    /// while held (more when doubled). Opponents (later) can call Blocks() and Strength.
    /// </summary>
    public class ShieldMove : Move
    {
        public Material shieldMaterial;
        public float drainPerSecond = 8f;
        public float minEnergyToStart = 5f;
        public float shieldRadius = 0.42f;
        [Header("Two-handed shield")]
        [Tooltip("Radius (meters) of the shield when both arms block.")]
        public float doubleShieldRadius = 0.75f;
        [Tooltip("Energy per second while both arms block.")]
        public float doubleDrainPerSecond = 12f;
        [Header("Enemy bolts")]
        [Tooltip("Ring energy a single shield GIVES BACK when it absorbs an enemy bolt (a reward for blocking). A double shield reflects bolts instead.")]
        public float absorbRefund = 2f;
        [Header("Pose rules")]
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

        private TrackedNode hand;     // first blocking hand
        private TrackedNode second;   // other hand when doubled, else null
        private GameObject disc;
        private readonly float[] pressTime = new float[2];
        private readonly bool[] wasHeld = new bool[2];

        public bool IsBlocking { get { return IsActive && disc != null; } }
        public bool IsDoubled { get { return IsBlocking && second != null; } }

        /// <summary>How strong the shield is: 1 normally, 2 when both arms block (for future enemy attacks).</summary>
        public float Strength { get { return IsDoubled ? 2f : (IsBlocking ? 1f : 0f); } }

        /// <summary>True if a world-space point is inside the shield disc (for future enemy attacks).</summary>
        public bool Blocks(Vector3 worldPoint)
        {
            if (!IsBlocking) return false;
            Vector3 local = disc.transform.InverseTransformPoint(worldPoint);
            // The disc is a flattened cylinder; local XZ is its face, local Y its thickness.
            return new Vector2(local.x, local.z).magnitude <= 0.5f && Mathf.Abs(local.y) <= 1.5f;
        }

        /// <summary>
        /// Did the path from 'from' to 'to' (a bolt of the given radius) cross the shield disc this frame?
        /// A plane-crossing test, because the disc is paper thin and bolts move fast.
        /// </summary>
        public bool Intercept(Vector3 from, Vector3 to, float boltRadius, out Vector3 point)
        {
            point = to;
            if (!IsBlocking) return false;
            Transform d = disc.transform;
            Vector3 n = d.up;   // the disc is tipped so its local Y faces forward
            Vector3 c = d.position;
            float da = Vector3.Dot(from - c, n);
            float db = Vector3.Dot(to - c, n);
            if ((da > 0f && db > 0f) || (da < 0f && db < 0f) || Mathf.Approximately(da, db)) return false;
            Vector3 p = from + (to - from) * (da / (da - db));
            Vector3 offset = p - c;
            offset -= n * Vector3.Dot(offset, n);
            if (offset.magnitude > d.localScale.x * 0.5f + boltRadius) return false;
            point = p;
            return true;
        }

        /// <summary>A single shield takes an enemy bolt and gives a little ring energy back.</summary>
        public bool AbsorbHit()
        {
            if (!IsBlocking) return false;
            ctx.energy.Add(absorbRefund);
            PulseHands(0.6f, 0.12f);
            return true;
        }

        /// <summary>Buzz the arm(s) holding the shield.</summary>
        public void PulseHands(float amplitude, float duration)
        {
            if (hand != null) hand.Haptic(amplitude, duration);
            if (second != null) second.Haptic(amplitude, duration);
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

        private int Index(TrackedNode h)
        {
            return h == ctx.Left ? 0 : 1;
        }

        public override bool TryStart(int freeHands)
        {
            bool l = (freeHands & LeftMask) != 0 && CanStart(ctx.Left);
            bool r = (freeHands & RightMask) != 0 && CanStart(ctx.Right);
            if (l && r) return Begin(ctx.Left, ctx.Right);
            if (l) return Begin(ctx.Left, null);
            if (r) return Begin(ctx.Right, null);
            return false;
        }

        private bool CanStart(TrackedNode h)
        {
            return h != null && h.IsTracked && h.TriggerHeld
                && Time.time - pressTime[Index(h)] >= holdDelay
                && Gestures.IsGuarding(ctx.Head, h, startMaxForwardDot)
                && h.Velocity.magnitude < startMaxSpeed
                && ctx.energy.CanAfford(minEnergyToStart);
        }

        private bool StillBlocking(TrackedNode h, out string reason)
        {
            reason = null;
            if (h == null || !h.IsTracked) reason = "lost tracking";
            else if (!h.TriggerHeld) reason = "trigger released";
            else if (!Gestures.IsGuarding(ctx.Head, h, stayMaxForwardDot)) reason = "left blocking pose";
            return reason == null;
        }

        private bool Begin(TrackedNode first, TrackedNode other)
        {
            hand = first;
            second = other;
            IsActive = true;
            disc = Fx.Shape(PrimitiveType.Cylinder, shieldMaterial, Vector3.one);
            disc.name = "Energy Shield";
            UpdateShape();
            hand.Haptic(0.3f, 0.1f);
            if (second != null) second.Haptic(0.3f, 0.1f);
            MoveLog.Add(second != null ? "Shield up (BOTH arms)" : "Shield " + MoveLog.HandName(first) + " up");
            return true;
        }

        public override void Tick()
        {
            if (!IsActive || hand == null) return;   // cancelled this frame (for example the shield broke)
            string reason;
            bool firstOk = StillBlocking(hand, out reason);
            string secondReason;
            bool secondOk = second != null && StillBlocking(second, out secondReason);

            if (!firstOk && secondOk)
            {
                // The first arm dropped but the second is still up: keep a single shield on it.
                hand = second;
                second = null;
                firstOk = true;
                MoveLog.Add("Shield back to one arm");
            }
            else if (second != null && !secondOk)
            {
                second = null;
                MoveLog.Add("Shield back to one arm");
            }

            if (!firstOk)
            {
                MoveLog.Add("Shield down: " + reason);
                Finish();
                return;
            }

            // Raise the other arm into the blocking pose to merge into one big shield.
            if (second == null)
            {
                TrackedNode other = hand == ctx.Left ? ctx.Right : ctx.Left;
                if (other != null && !ctx.controller.IsHandBusy(other.HandMask) && CanStart(other))
                {
                    second = other;
                    other.Haptic(0.5f, 0.12f);
                    hand.Haptic(0.5f, 0.12f);
                    MoveLog.Add("Shield DOUBLED (both arms)");
                }
            }

            if (!ctx.energy.Drain(second != null ? doubleDrainPerSecond : drainPerSecond))
            {
                MoveLog.Add("Shield down: out of energy");
                Finish();
                return;
            }
            UpdateShape();
        }

        private void UpdateShape()
        {
            if (disc == null || hand == null) return;
            UsedHands = hand.HandMask | (second != null ? second.HandMask : 0);

            float radius = second != null ? doubleShieldRadius : shieldRadius;
            disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            // Face the barrier the way the player is facing, so it covers them no matter how the wrist is turned.
            Vector3 forward = Gestures.FlatForward(ctx.Head);
            Vector3 center = second != null ? (hand.transform.position + second.transform.position) * 0.5f : hand.transform.position;
            disc.transform.position = center + forward * distanceFromHand;
            // Cylinders stand along their local Y axis, so tip it to face forward.
            disc.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90f, 0f, 0f);
        }

        protected override void OnFinish()
        {
            if (disc != null) Destroy(disc);
            disc = null;
            hand = null;
            second = null;
        }
    }
}
