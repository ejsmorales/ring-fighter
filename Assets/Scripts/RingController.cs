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

        // Counts how many lasting moves have claimed each hand (index 0 = left, 1 = right).
        // Lets the energy bolt tell "I tapped the trigger" apart from "that trigger press became something else".
        private readonly int[] handClaims = new int[2];

        public int GetHandClaims(int handMask)
        {
            return handClaims[handMask == Move.LeftMask ? 0 : 1];
        }

        private void Start()
        {
            RingContext ctx = new RingContext { rig = rig, energy = energy, controller = this };
            GetComponents(allMoves);
            allMoves.Sort((a, b) => b.priority.CompareTo(a.priority));
            for (int i = 0; i < allMoves.Count; i++)
                allMoves[i].Init(ctx);
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
                    if ((m.UsedHands & Move.LeftMask) != 0) handClaims[0]++;
                    if ((m.UsedHands & Move.RightMask) != 0) handClaims[1]++;
                    busy |= m.UsedHands;
                    free = Move.BothMask & ~busy;
                }
            }
        }
    }
}
