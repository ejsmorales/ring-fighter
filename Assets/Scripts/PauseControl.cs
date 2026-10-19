using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Pause and resume with the menu button (three lines) on the LEFT controller. While paused, game time stops
    /// (the dummy, bolts, energy), no ring moves run, and a "PAUSED" sign floats in front of you. The debug panel
    /// still works (Y toggles it). Pausing never resets anything.
    /// </summary>
    public class PauseControl : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        public RingRig rig;
        [Tooltip("How far in front of you (meters) the pause sign floats.")]
        public float signDistance = 1.5f;

        private bool wasHeld = true;   // a button already down at launch doesn't count
        private GameObject sign;

        private void Update()
        {
            TrackedNode left = rig != null ? rig.leftHand : null;
            bool held = left != null && left.MenuButton;
            if (held && !wasHeld) SetPaused(!IsPaused);
            wasHeld = held;
        }

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;

            if (paused) ShowSign();
            else HideSign();
            MoveLog.Add(paused ? "PAUSED" : "Resumed");
        }

        private void ShowSign()
        {
            sign = new GameObject("Pause Sign");
            TextMesh tm = sign.AddComponent<TextMesh>();
            tm.text = "PAUSED\n\nMenu button (left) to resume";
            Fx.SetupText(tm, 0.08f, new Color(0.7f, 1f, 0.8f));
            if (rig != null && rig.head != null)
            {
                Vector3 fwd = Gestures.FlatForward(rig.head);
                sign.transform.position = rig.head.transform.position + fwd * signDistance;
                sign.transform.rotation = Quaternion.LookRotation(fwd);
            }
        }

        private void HideSign()
        {
            if (sign != null) Destroy(sign);
            sign = null;
        }

        private void OnDestroy()
        {
            // Never leave the game frozen if this object goes away while paused.
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }
    }
}
