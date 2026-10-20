using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RingFighter
{
    /// <summary>
    /// A floating panel off to the player's front-left that shows what the gesture system sees, live:
    /// per hand trigger pressure, speed, pose (aiming / blocking), circling angle; active moves; and a log of
    /// recent move events including why a move didn't fire. Press Y (left controller) to hide or show it.
    /// Created automatically by RingController when "Show Debug Readout" is ticked.
    /// </summary>
    public class DebugReadout : MonoBehaviour
    {
        public Vector3 worldPosition = new Vector3(-1.5f, 1.45f, 2.0f);
        public float refreshInterval = 0.1f;

        private RingContext ctx;
        private LaserMove laser;
        private ShieldMove shield;
        private PlayerMovement movement;
        private Text text;
        private GameObject panel;
        private float nextRefresh;
        private bool yWasHeld;
        private bool aWasHeld;
        private bool bWasHeld;
        private readonly StringBuilder sb = new StringBuilder(1024);

        public void Init(RingContext context)
        {
            ctx = context;
            laser = ctx.controller.GetComponent<LaserMove>();
            shield = ctx.controller.GetComponent<ShieldMove>();
            movement = FindAnyObjectByType<PlayerMovement>();
            Build();
        }

        private void Build()
        {
            transform.position = worldPosition;
            // Face the player's starting spot.
            Vector3 away = worldPosition - new Vector3(0f, worldPosition.y, 0f);
            transform.rotation = Quaternion.LookRotation(away.sqrMagnitude > 0.01f ? away : Vector3.forward);

            panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            Canvas canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620f, 700f);
            rt.localScale = Vector3.one * 0.0017f;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(panel.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18f, 14f);
            trt.offsetMax = new Vector2(-18f, -14f);
            text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.lineSpacing = 1.05f;
            text.color = new Color(0.75f, 1f, 0.8f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void Update()
        {
            if (ctx == null || text == null) return;

            // Y on the left controller toggles the panel.
            bool y = ctx.Left != null && ctx.Left.SecondaryButton;
            if (y && !yWasHeld) panel.SetActive(!panel.activeSelf);
            yWasHeld = y;

            // While the panel is showing: A (right controller) tilts the aim down 5 more degrees, B tilts it back up.
            // Use it to find the angle where a relaxed wrist points at the dummy. The value is remembered.
            if (panel.activeSelf && ctx.Right != null)
            {
                bool a = ctx.Right.PrimaryButton;
                bool b = ctx.Right.SecondaryButton;
                if (a && !aWasHeld) AdjustAimPitch(5f);
                if (b && !bWasHeld) AdjustAimPitch(-5f);
                aWasHeld = a;
                bWasHeld = b;
            }

            if (!panel.activeSelf || Time.time < nextRefresh) return;
            nextRefresh = Time.time + refreshInterval;
            text.text = Compose();
        }

        private void AdjustAimPitch(float delta)
        {
            float value = Mathf.Clamp(ctx.Right.aimPitchOffset + delta, 0f, 120f);
            if (ctx.Left != null) ctx.Left.aimPitchOffset = value;
            ctx.Right.aimPitchOffset = value;
            PlayerPrefs.SetFloat(TrackedNode.AimPitchPrefKey, value);
            PlayerPrefs.Save();
            MoveLog.Add("Aim tilt set to " + Mathf.RoundToInt(value));
        }

        private string Compose()
        {
            sb.Length = 0;
            sb.Append("RING DEBUG   (Y = hide)\n");
            if (!string.IsNullOrEmpty(MoveLog.Status)) sb.Append(MoveLog.Status).Append('\n');
            if (ctx.Right != null)
                sb.Append("Aim tilt ").Append(Mathf.RoundToInt(ctx.Right.aimPitchOffset)).Append("  (A = more, B = less)\n");
            if (movement != null) sb.Append("Move: ").Append(movement.StatusText()).Append('\n');
            sb.Append("Energy ").Append(Mathf.RoundToInt(ctx.energy.Current)).Append(" / ").Append(Mathf.RoundToInt(ctx.energy.max)).Append('\n');

            if (ctx.RingHand == null) sb.Append("Ring hand: not picked yet (pull a trigger)\n");
            else
            {
                sb.Append("Ring hand: ").Append(ctx.RingHand == ctx.Left ? "LEFT" : "RIGHT");
                if (laser != null)
                {
                    float gap = laser.HandGap();
                    sb.Append("   focus: ").Append(laser.IsFocused() ? "YES" : "no").Append(" (").Append(gap.ToString("0.00")).Append(" m)");
                }
                sb.Append('\n');
            }
            sb.Append("Active: ");
            var active = ctx.controller.ActiveMoves;
            if (active.Count == 0) sb.Append("none");
            for (int i = 0; i < active.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(active[i].GetType().Name.Replace("Move", ""));
            }
            sb.Append("\n\n");

            HandLines("LEFT", ctx.Left);
            HandLines("RIGHT", ctx.Right);

            if (ctx.Left != null && ctx.Right != null)
            {
                float gap = (ctx.Left.transform.position - ctx.Right.transform.position).magnitude;
                sb.Append("Hands apart ").Append(gap.ToString("0.00")).Append(" m\n");
            }

            sb.Append("\nRecent:\n");
            for (int i = 0; i < MoveLog.Size; i++)
            {
                string e = MoveLog.Get(i);
                if (e == null) break;
                sb.Append(e).Append('\n');
            }
            return sb.ToString();
        }

        private void HandLines(string label, TrackedNode hand)
        {
            sb.Append(label);
            if (hand == null || !hand.IsTracked)
            {
                sb.Append(": not tracked\n\n");
                return;
            }

            float fwd = Gestures.ForwardDot(ctx.Head, hand);
            string pose = "neutral";
            if (laser != null && fwd >= laser.minForwardDot) pose = "AIMING";
            else if (shield != null && Gestures.IsGuarding(ctx.Head, hand, shield.startMaxForwardDot)) pose = "BLOCKING";

            float angle;
            Gestures.IsCircling(hand, 0.85f, 99999f, out angle);

            sb.Append(": ").Append(pose).Append("  (fwd ").Append(fwd.ToString("0.00")).Append(")\n");
            sb.Append("  trigger ").Append(hand.Trigger.ToString("0.00"));
            sb.Append("  speed ").Append(hand.Velocity.magnitude.ToString("0.0")).Append(" m/s");
            sb.Append("  circle ").Append(Mathf.RoundToInt(angle)).Append("°\n\n");
        }
    }
}
