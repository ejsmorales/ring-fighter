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

        [Tooltip("Show the floating debug readout (press Y on the left controller to hide/show in game).")]
        public bool showDebugReadout = true;

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
            RingContext ctx = new RingContext { rig = rig, energy = energy, controller = this };
            GetComponents(allMoves);
            allMoves.Sort((a, b) => b.priority.CompareTo(a.priority));
            for (int i = 0; i < allMoves.Count; i++)
                allMoves[i].Init(ctx);

            if (showDebugReadout)
            {
                DebugReadout readout = new GameObject("Debug Readout").AddComponent<DebugReadout>();
                readout.Init(ctx);
            }
        }

        private void Update()
        {
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
