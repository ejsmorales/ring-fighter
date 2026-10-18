using UnityEngine;

namespace RingFighter
{
    /// <summary>What every move needs to know about the player.</summary>
    public class RingContext
    {
        public RingRig rig;
        public RingEnergy energy;
        public RingController controller;

        public TrackedNode Head { get { return rig.head; } }
        public TrackedNode Left { get { return rig.leftHand; } }
        public TrackedNode Right { get { return rig.rightHand; } }

        /// <summary>The hand wearing the ring (main attacks). Null until the player picks it at startup.</summary>
        public TrackedNode RingHand { get; set; }

        /// <summary>The other hand: support (shield, focus). Null until the ring hand is picked.</summary>
        public TrackedNode OffHand
        {
            get
            {
                if (RingHand == null) return null;
                return RingHand == rig.leftHand ? rig.rightHand : rig.leftHand;
            }
        }
    }

    /// <summary>
    /// Base class for a ring move (energy bolt, shield, ...). To add a new move:
    ///   1. Make a new class that extends Move.
    ///   2. Fill in TryStart (recognize your gesture) and, if the move lasts, Tick.
    ///   3. Add the component next to RingController - it finds it automatically.
    /// Hands are tracked as a bit mask: 1 = left, 2 = right, 3 = both. A move only
    /// starts if the hands it needs are free, so you can shield with one hand and fire with the other.
    /// </summary>
    public abstract class Move : MonoBehaviour
    {
        public const int LeftMask = 1;
        public const int RightMask = 2;
        public const int BothMask = 3;

        [Tooltip("Higher priority moves are checked first when a gesture could be several things.")]
        public int priority = 0;

        public bool IsActive { get; protected set; }
        public int UsedHands { get; protected set; }

        protected RingContext ctx;

        public void Init(RingContext context)
        {
            ctx = context;
            OnInit();
        }

        protected virtual void OnInit() { }

        /// <summary>Called every frame for every move, even while another move is running. Use it to track button state.</summary>
        public virtual void Observe() { }

        /// <summary>
        /// Called when some hands are free. Return true if your gesture was recognized and the move began.
        /// For a lasting move, set IsActive = true and UsedHands. For an instant move (fires once), do the work
        /// and leave IsActive false.
        /// </summary>
        public abstract bool TryStart(int freeHands);

        /// <summary>Called each frame while IsActive. Call Finish() when the move is over.</summary>
        public virtual void Tick() { }

        /// <summary>Force the move to stop (for example if the player is knocked out).</summary>
        public virtual void Cancel()
        {
            Finish();
        }

        protected virtual void OnFinish() { }

        protected void Finish()
        {
            if (!IsActive && UsedHands == 0) return;
            IsActive = false;
            UsedHands = 0;
            OnFinish();
        }
    }
}
