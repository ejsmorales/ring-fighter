using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Moves the player around the arena with the OFF-hand thumbstick (ring on the left hand = move with the right
    /// stick, and vice versa). Independent of the ring moves: it only moves the rig (the player's root), so hit
    /// tests, the HUD and the dummy's aim all follow automatically. Real steps in your room still work on top of it.
    ///
    /// Modes (click the off-hand stick to cycle; the choice is remembered):
    ///   Dash   - flick the stick: a quick burst in that direction, then a short cooldown. Best for dodging bolts.
    ///   Smooth - hold the stick to glide. More flexible, but some people get dizzy.
    ///   Off    - no stick movement (physical dodging only).
    /// Directions are relative to where your head faces.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        public enum Mode { Dash, Smooth, Off }

        public const string ModePrefKey = "RF_MoveMode";

        public RingRig rig;
        public RingController controller;
        public PlayerHealth health;

        public Mode mode = Mode.Dash;

        [Header("Dash")]
        [Tooltip("How far one dash goes (meters).")]
        public float dashDistance = 1.4f;
        [Tooltip("How long the burst lasts (seconds). Shorter = snappier.")]
        public float dashTime = 0.14f;
        [Tooltip("Seconds before you can dash again (counted from the end of the dash).")]
        public float dashCooldown = 0.45f;
        [Tooltip("Ring energy one dash costs. 0 = free.")]
        public float dashEnergyCost = 0f;
        [Tooltip("How far the stick must be pushed (0-1) to count as a flick.")]
        public float flickThreshold = 0.7f;

        [Header("Smooth")]
        [Tooltip("Walking speed (m/s) at full stick.")]
        public float smoothSpeed = 2.2f;
        [Tooltip("Ignore tiny stick movements below this (0-1).")]
        public float deadzone = 0.15f;

        [Header("Limits")]
        [Tooltip("The arena radius (meters) you can move out to from the middle. Stops you walking off or into the dummy.")]
        public float arenaRadius = 4f;

        private bool dashing;
        private float dashStart;
        private float dashEnd;
        private Vector3 dashFrom, dashTo;
        private float nextDashTime;
        private bool flickHeld = true;     // a stick already pushed at launch doesn't count
        private bool clickWasHeld = true;
        private Vector3 startPosition;

        private void Awake()
        {
            if (PlayerPrefs.HasKey(ModePrefKey))
                mode = (Mode)Mathf.Clamp(PlayerPrefs.GetInt(ModePrefKey), 0, 2);
        }

        private void Start()
        {
            if (rig != null) startPosition = rig.transform.position;
        }

        public string StatusText()
        {
            string s = mode.ToString().ToUpper();
            if (mode == Mode.Dash && dashing) s += " (dashing)";
            else if (mode == Mode.Dash && Time.time < nextDashTime) s += " (cooldown)";
            return s + "   (click off-hand stick to switch)";
        }

        /// <summary>Back to the starting spot (used when the fight restarts).</summary>
        public void ResetPosition()
        {
            dashing = false;
            if (rig != null) rig.transform.position = startPosition;
        }

        private TrackedNode StickHand()
        {
            RingContext ctx = controller != null ? controller.Context : null;
            return ctx != null ? ctx.OffHand : null;   // null until the ring hand is picked
        }

        private void Update()
        {
            if (rig == null || PauseControl.IsPaused) return;
            TrackedNode stickHand = StickHand();
            if (stickHand == null || !stickHand.IsTracked) return;

            // Click the stick to change mode.
            bool click = stickHand.ThumbstickClick;
            if (click && !clickWasHeld) CycleMode(stickHand);
            clickWasHeld = click;

            if (health != null && !health.IsAlive)
            {
                dashing = false;
                return;
            }

            Vector2 stick = stickHand.Thumbstick;

            if (dashing)
            {
                UpdateDash();
                return;
            }

            if (mode == Mode.Dash) TryDash(stick, stickHand);
            else if (mode == Mode.Smooth) Glide(stick);
        }

        private void CycleMode(TrackedNode hand)
        {
            mode = (Mode)(((int)mode + 1) % 3);
            dashing = false;
            PlayerPrefs.SetInt(ModePrefKey, (int)mode);
            PlayerPrefs.Save();
            hand.Haptic(0.4f, 0.08f);
            MoveLog.Add("Move mode: " + mode);
        }

        // Head-relative direction on the floor for a stick reading (x = right, y = forward).
        private Vector3 WorldDirection(Vector2 stick)
        {
            Vector3 fwd = rig.FlatForward();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            return fwd * stick.y + right * stick.x;
        }

        private void TryDash(Vector2 stick, TrackedNode hand)
        {
            bool pushed = stick.magnitude >= flickThreshold;
            bool rising = pushed && !flickHeld;
            flickHeld = pushed;
            if (!rising || Time.time < nextDashTime) return;
            if (dashEnergyCost > 0f && controller != null && !controller.energy.TrySpend(dashEnergyCost))
            {
                MoveLog.Add("Dash skipped: no energy");
                return;
            }

            Vector3 dir = WorldDirection(stick);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            dashFrom = rig.transform.position;
            dashTo = Clamp(dashFrom + dir * dashDistance);
            dashStart = Time.time;
            dashEnd = dashStart + Mathf.Max(0.02f, dashTime);
            dashing = true;

            hand.Haptic(0.35f, 0.06f);
            if (rig.GetHand(hand.HandMask == 1 ? 2 : 1) != null) rig.GetHand(hand.HandMask == 1 ? 2 : 1).Haptic(0.2f, 0.06f);
            MoveLog.Add("Dash");
        }

        private void UpdateDash()
        {
            float t = Mathf.InverseLerp(dashStart, dashEnd, Time.time);
            float eased = t * t * (3f - 2f * t);
            Vector3 p = Vector3.Lerp(dashFrom, dashTo, eased);
            p.y = rig.transform.position.y;
            rig.transform.position = p;
            if (t >= 1f)
            {
                dashing = false;
                nextDashTime = Time.time + dashCooldown;
                flickHeld = true;   // must let go of the stick and flick again
            }
        }

        private void Glide(Vector2 stick)
        {
            float mag = stick.magnitude;
            if (mag < deadzone) return;
            Vector3 dir = WorldDirection(stick);
            dir.y = 0f;
            Vector3 p = rig.transform.position + dir * (smoothSpeed * Time.deltaTime);
            p.y = rig.transform.position.y;
            rig.transform.position = Clamp(p);
        }

        // Keeps the rig inside the arena circle (around the world origin).
        private Vector3 Clamp(Vector3 p)
        {
            Vector2 flat = new Vector2(p.x, p.z);
            if (flat.magnitude > arenaRadius)
            {
                flat = flat.normalized * arenaRadius;
                p.x = flat.x;
                p.z = flat.y;
            }
            return p;
        }
    }
}
