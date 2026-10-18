using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// A short list of recent move events ("Bolt R fired", "Bolt R skipped: hand moving"), shown on the
    /// debug readout. Moves call MoveLog.Add; it costs nothing when nobody reads it.
    /// </summary>
    public static class MoveLog
    {
        public const int Size = 7;
        private static readonly string[] entries = new string[Size];
        private static int head;
        private static int count;

        /// <summary>One line pinned at the top of the debug readout (for example the startup physics check).</summary>
        public static string Status;

        public static void Add(string message)
        {
            entries[head] = Time.time.ToString("0.0") + "s  " + message;
            head = (head + 1) % Size;
            if (count < Size) count++;
        }

        /// <summary>Newest first.</summary>
        public static string Get(int i)
        {
            if (i >= count) return null;
            int idx = (head - 1 - i + Size * 2) % Size;
            return entries[idx];
        }

        public static string HandName(TrackedNode hand)
        {
            return hand != null && hand.node == TrackedNode.NodeType.LeftHand ? "L" : "R";
        }
    }
}
