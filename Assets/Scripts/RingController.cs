using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Runs the player's moves. Finds every Move component on this object, gives each one a
    /// chance to recognize its gesture, and keeps track of which hands are busy.
    /// </summary>
    public class RingController : MonoBehaviour
    {
        public RingRig rig;
        public RingEnergy energy;

        private readonly List<Move> allMoves = new List<Move>();
        private readonly List<Move> activeMoves = new List<Move>();
        private RingContext ctx;

        [Tooltip("Distance in front of the player (meters) of the 'pick your ring hand' prompt.")]
        public float promptDistance = 1.4f;

        private GameObject prompt;
        private bool leftWasHeld = true;    // start as "held" so a trigger already down at launch doesn't count
        private bool rightWasHeld = true;

        [Tooltip("Show the floating debug readout (press Y on the left controller to hide/show in game).")]
        public bool showDebugReadout = true;

        private PlayerHealth health;

        /// <summary>Stop everything that is running (shield, laser...). Used when the player is knocked out.</summary>
        public void CancelAllMoves()
        {
            for (int i = activeMoves.Count - 1; i >= 0; i--) activeMoves[i].Cancel();
            activeMoves.Clear();
        }

        /// <summary>The shared player info (ring hand, off hand...). Null before Start.</summary>
        public RingContext Context { get { return ctx; } }

        public IReadOnlyList<Move> ActiveMoves { get { return activeMoves; } }

        /// <summary>True if a lasting move (shield, whirlwind, blast) is currently using this hand.</summary>
        public bool IsHandBusy(int handMask)
        {
            for (int i = 0; i < activeMoves.Count; i++)
                if ((activeMoves[i].UsedHands & handMask) != 0) return true;
            return false;
        }

        private void Start()
        {
            ctx = new RingContext { rig = rig, energy = energy, controller = this };
            GetComponents(allMoves);
            allMoves.Sort((a, b) => b.priority.CompareTo(a.priority));
            for (int i = 0; i < allMoves.Count; i++)
                allMoves[i].Init(ctx);

            if (showDebugReadout)
            {
                DebugReadout readout = new GameObject("Debug Readout").AddComponent<DebugReadout>();
                readout.Init(ctx);
            }

            health = FindAnyObjectByType<PlayerHealth>();
            ShowPrompt();
        }

        // ---- Picking the ring hand (every launch) ----

        private void ShowPrompt()
        {
            prompt = new GameObject("Ring Hand Prompt");
            TextMesh tm = prompt.AddComponent<TextMesh>();
            tm.text = "Pull the trigger on the hand\nthat wears the ring";
            Fx.SetupText(tm, 0.07f, new Color(0.4f, 1f, 0.6f));
            PlacePrompt();
        }

        private void PlacePrompt()
        {
            if (prompt == null || rig == null || rig.head == null) return;
            Vector3 fwd = Gestures.FlatForward(rig.head);
            prompt.transform.position = rig.head.transform.position + fwd * promptDistance;
            prompt.transform.rotation = Quaternion.LookRotation(fwd);
        }

        private void UpdatePicker()
        {
            PlacePrompt();
            TrackedNode chosen = null;
            bool l = ctx.Left != null && ctx.Left.TriggerHeld;
            bool r = ctx.Right != null && ctx.Right.TriggerHeld;
            if (l && !leftWasHeld) chosen = ctx.Left;
            else if (r && !rightWasHeld) chosen = ctx.Right;
            leftWasHeld = l;
            rightWasHeld = r;
            if (chosen == null) return;

            ctx.RingHand = chosen;
            if (health != null) health.FightStarted = true;
            TrackedNode off = ctx.OffHand;

            // Only the ring hand shows the ring.
            SetRingVisible(chosen, true);
            SetRingVisible(off, false);

            // The energy meter floats over the support hand, if the scene still has one (the HUD shows it now).
            EnergyBarVisual bar = FindAnyObjectByType<EnergyBarVisual>();
            if (bar != null && off != null) bar.follow = off.transform;

            chosen.Haptic(0.8f, 0.25f);
            MoveLog.Add("Ring hand: " + (chosen == ctx.Left ? "LEFT" : "RIGHT"));
            if (prompt != null) Destroy(prompt);
            prompt = null;
        }

        private static void SetRingVisible(TrackedNode hand, bool visible)
        {
            if (hand == null) return;
            foreach (string part in new[] { "Ring", "Ring Gem" })
            {
                Transform t = hand.transform.Find(part);
                if (t != null) t.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            if (ctx == null) return;
            if (PauseControl.IsPaused) return;   // game paused: no moves
            if (ctx.RingHand == null)
            {
                UpdatePicker();
                // No moves until the ring hand is picked. The laser also ignores the trigger pull used to pick.
                return;
            }

            // Knocked out: no moves until the fight restarts.
            if (health != null && !health.IsAlive) return;

            for (int i = 0; i < allMoves.Count; i++)
                allMoves[i].Observe();

            for (int i = activeMoves.Count - 1; i >= 0; i--)
            {
                Move m = activeMoves[i];
                m.Tick();
                if (!m.IsActive)
                    activeMoves.RemoveAt(i);
            }

            int busy = 0;
            for (int i = 0; i < activeMoves.Count; i++)
                busy |= activeMoves[i].UsedHands;

            int free = Move.BothMask & ~busy;
            if (free == 0) return;

            for (int i = 0; i < allMoves.Count && free != 0; i++)
            {
                Move m = allMoves[i];
                if (m.IsActive) continue;
                if (!m.TryStart(free)) continue;

                if (m.IsActive)
                {
                    activeMoves.Add(m);
                    busy |= m.UsedHands;
                    free = Move.BothMask & ~busy;
                }
            }
        }
    }
}
