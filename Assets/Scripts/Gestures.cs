using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Pure gesture-recognition helpers. Each answers one question about the hands.
    /// Tweak the numbers here to change how strict a gesture is.
    /// </summary>
    public static class Gestures
    {
        private static readonly Vector3[] buffer = new Vector3[160];

        /// <summary>
        /// Is this hand tracing a circle? Looks at the last 'window' seconds of hand positions
        /// and measures how far around a circle they went. Works for any orientation of the circle.
        /// </summary>
        public static bool IsCircling(TrackedNode hand, float window, float minAngleDegrees, out float angleDegrees)
        {
            angleDegrees = 0f;
            int n = hand.CopyHistory(window, buffer);
            if (n < 12) return false;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < n; i++) center += buffer[i];
            center /= n;

            float meanRadius = 0f;
            for (int i = 0; i < n; i++) meanRadius += (buffer[i] - center).magnitude;
            meanRadius /= n;
            if (meanRadius < 0.06f || meanRadius > 0.6f) return false;

            // A real circle stays roughly the same distance from its center; a back-and-forth
            // line passes through the center, so reject paths that come too close to it.
            for (int i = 0; i < n; i++)
            {
                if ((buffer[i] - center).magnitude < meanRadius * 0.45f) return false;
            }

            Vector3 normal = Vector3.zero;
            for (int i = 0; i < n - 1; i++)
                normal += Vector3.Cross(buffer[i] - center, buffer[i + 1] - center);
            if (normal.sqrMagnitude < 1e-8f) return false;
            normal.Normalize();

            float total = 0f;
            for (int i = 0; i < n - 1; i++)
                total += Vector3.SignedAngle(buffer[i] - center, buffer[i + 1] - center, normal);

            angleDegrees = Mathf.Abs(total);
            return angleDegrees >= minAngleDegrees;
        }

        /// <summary>Direction the player's head faces, flattened onto the ground.</summary>
        public static Vector3 FlatForward(TrackedNode head)
        {
            Vector3 f = head.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) return Vector3.forward;
            return f.normalized;
        }

        /// <summary>
        /// How much the controller points the way the player faces: 1 = straight ahead, 0 = up/down/sideways.
        /// Pointing at the opponent is near 1; a raised blocking forearm points up or across, near 0.
        /// </summary>
        public static float ForwardDot(TrackedNode head, TrackedNode hand)
        {
            return Vector3.Dot(hand.AimForward, FlatForward(head));
        }

        /// <summary>
        /// Is the hand in a blocking pose? In front of the body, at chest height or above, and the controller
        /// is NOT pointing forward (it points up or sideways, like a raised forearm). That last part is what
        /// keeps "aiming at the opponent" from counting as a block.
        /// </summary>
        public static bool IsGuarding(TrackedNode head, TrackedNode hand, float maxForwardDot)
        {
            Vector3 rel = hand.transform.position - head.transform.position;
            float ahead = Vector3.Dot(rel, FlatForward(head));
            return ahead > 0.08f && rel.y > -0.45f && ForwardDot(head, hand) < maxForwardDot;
        }

        /// <summary>
        /// Are both hands held together off to one side of the body, like drawing back an energy ball?
        /// </summary>
        public static bool HandsTogetherAtSide(TrackedNode head, TrackedNode left, TrackedNode right, float maxGap, float minSideOffset)
        {
            Vector3 l = left.transform.position;
            Vector3 r = right.transform.position;
            if ((l - r).magnitude > maxGap) return false;

            Vector3 mid = (l + r) * 0.5f;
            Vector3 headRight = head.transform.right;
            headRight.y = 0f;
            if (headRight.sqrMagnitude < 0.0001f) return false;
            headRight.Normalize();

            float side = Vector3.Dot(mid - head.transform.position, headRight);
            return Mathf.Abs(side) >= minSideOffset;
        }
    }
}
