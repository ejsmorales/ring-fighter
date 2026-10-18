using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Hit detection that doesn't depend on Unity's physics colliders. Each target registers an upright capsule
    /// (feet at its transform, a height and a radius); shots test their path against these capsules with plain math.
    /// Attacks use this alongside physics, so targets get hit even if their colliders are missing in a build.
    /// </summary>
    public static class HitShapes
    {
        private class Entry
        {
            public IDamageable owner;
            public Transform root;
            public float radius;
            public float height;
        }

        private static readonly List<Entry> entries = new List<Entry>();

        public static void Register(IDamageable owner, Transform root, float radius, float height)
        {
            entries.Add(new Entry { owner = owner, root = root, radius = radius, height = height });
        }

        public static void Unregister(IDamageable owner)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].owner == owner) entries.RemoveAt(i);
        }

        /// <summary>
        /// Does the path from a to b (thickened by 'thickness') touch a live target? Returns the first one along the
        /// path: 'fraction' is how far along a..b it was hit (0..1), 'point' is a spot on the target's surface.
        /// </summary>
        public static bool Segment(Vector3 a, Vector3 b, float thickness, out IDamageable target, out Vector3 point, out float fraction)
        {
            target = null;
            point = b;
            fraction = 1f;
            float best = float.MaxValue;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.root == null || e.owner == null || !e.owner.IsAlive) continue;
                Vector3 bottom, top;
                Axis(e, out bottom, out top);

                float s, t;
                Vector3 onPath, onAxis;
                float dist = ClosestSegSeg(a, b, bottom, top, out s, out t, out onPath, out onAxis);
                if (dist > e.radius + thickness || s >= best) continue;

                best = s;
                target = e.owner;
                fraction = s;
                Vector3 outward = onPath - onAxis;
                point = outward.sqrMagnitude > 1e-6f ? onAxis + outward.normalized * e.radius : onPath;
            }
            return target != null;
        }

        /// <summary>Adds every live target whose capsule touches the sphere to 'results' (no duplicates).</summary>
        public static void Overlap(Vector3 center, float radius, List<IDamageable> results, List<Vector3> points)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.root == null || e.owner == null || !e.owner.IsAlive || results.Contains(e.owner)) continue;
                Vector3 bottom, top;
                Axis(e, out bottom, out top);
                Vector3 onAxis = ClosestOnSegment(bottom, top, center);
                Vector3 outward = center - onAxis;
                if (outward.magnitude > radius + e.radius) continue;
                results.Add(e.owner);
                points.Add(outward.sqrMagnitude > 1e-6f ? onAxis + outward.normalized * e.radius : onAxis);
            }
        }

        private static void Axis(Entry e, out Vector3 bottom, out Vector3 top)
        {
            Vector3 up = Vector3.up;
            Vector3 feet = e.root.position;
            bottom = feet + up * e.radius;
            top = feet + up * Mathf.Max(e.radius, e.height - e.radius);
        }

        private static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-8f) return a;
            return a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        }

        // Closest points between segments p1-q1 and p2-q2 (Ericson, Real-Time Collision Detection 5.1.9).
        private static float ClosestSegSeg(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,
                                           out float s, out float t, out Vector3 c1, out Vector3 c2)
        {
            const float eps = 1e-6f;
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            s = 0f;
            t = 0f;

            if (a <= eps && e <= eps)
            {
                // both segments are points
            }
            else if (a <= eps)
            {
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= eps)
                {
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > eps ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
            return (c1 - c2).magnitude;
        }
    }
}
