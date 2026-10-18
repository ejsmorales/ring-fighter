using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Gentle aim help: if a shot is aimed within a few degrees of a target, it's redirected straight at it.
    /// Targets register an aim point (for example the dummy's chest).
    /// </summary>
    public static class AimAssist
    {
        private struct Entry
        {
            public Transform point;
            public IDamageable owner;
        }

        private static readonly List<Entry> targets = new List<Entry>();

        public static void Register(Transform point, IDamageable owner)
        {
            targets.Add(new Entry { point = point, owner = owner });
        }

        public static void Unregister(Transform point)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
                if (targets[i].point == point) targets.RemoveAt(i);
        }

        /// <summary>
        /// Returns a direction from origin. If a live target lies within maxAngle degrees of 'direction',
        /// returns the direction straight at the closest such target; otherwise returns 'direction' unchanged.
        /// </summary>
        public static Vector3 Adjust(Vector3 origin, Vector3 direction, float maxAngle, out bool assisted)
        {
            assisted = false;
            Vector3 best = direction;
            float bestAngle = maxAngle;
            for (int i = 0; i < targets.Count; i++)
            {
                Entry e = targets[i];
                if (e.point == null || e.owner == null || !e.owner.IsAlive) continue;
                Vector3 to = e.point.position - origin;
                if (to.sqrMagnitude < 0.01f) continue;
                float angle = Vector3.Angle(direction, to);
                if (angle <= bestAngle)
                {
                    bestAngle = angle;
                    best = to.normalized;
                    assisted = true;
                }
            }
            return best;
        }
    }
}
