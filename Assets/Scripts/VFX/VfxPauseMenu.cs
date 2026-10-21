using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// A small panel under the "PAUSED" sign for the visual-effect settings. While paused, press A (right
    /// controller) to cycle the effects quality (High, Low, Minimal) and B to switch real bloom on or off.
    /// The choice is remembered on the headset. Changes apply to the next shot or hit you make after resuming.
    /// </summary>
    public class VfxPauseMenu : MonoBehaviour
    {
        [Tooltip("How far in front of you (meters) the panel floats. Matches the pause sign.")]
        public float distance = 1.5f;
        [Tooltip("How far below the pause sign (meters) the panel sits.")]
        public float dropBelowSign = 0.42f;

        private RingRig rig;
        private GameObject panel;
        private TextMesh text;
        private bool aWasHeld;
        private bool bWasHeld;

        private void Update()
        {
            if (PauseControl.IsPaused)
            {
                if (panel == null) Show();
                Handle();
            }
            else if (panel != null)
            {
                Hide();
            }
        }

        private void Show()
        {
            if (rig == null) rig = FindAnyObjectByType<RingRig>();
            if (rig == null || rig.head == null) return;

            panel = new GameObject("VFX Menu");
            text = panel.AddComponent<TextMesh>();
            Fx.SetupText(text, 0.05f, new Color(0.7f, 1f, 0.8f));

            Vector3 fwd = Gestures.FlatForward(rig.head);
            panel.transform.position = rig.head.transform.position + fwd * distance + Vector3.down * dropBelowSign;
            panel.transform.rotation = Quaternion.LookRotation(fwd);

            // A button already held when the menu opens doesn't count.
            TrackedNode right = rig.rightHand;
            aWasHeld = right != null && right.PrimaryButton;
            bWasHeld = right != null && right.SecondaryButton;
            Refresh();
        }

        private void Hide()
        {
            if (panel != null) Destroy(panel);
            panel = null;
            text = null;
        }

        private void Handle()
        {
            if (panel == null || rig == null) return;
            TrackedNode right = rig.rightHand;
            if (right == null) return;

            bool a = right.PrimaryButton;
            bool b = right.SecondaryButton;
            if (a && !aWasHeld)
            {
                VfxSettings.CycleQuality();
                right.Haptic(0.3f, 0.05f);
                MoveLog.Add("VFX quality: " + VfxSettings.QualityLabel);
                Refresh();
            }
            if (b && !bWasHeld)
            {
                VfxSettings.ToggleBloom();
                right.Haptic(0.3f, 0.05f);
                Refresh();
            }
            aWasHeld = a;
            bWasHeld = b;
        }

        private void Refresh()
        {
            if (text == null) return;
            text.text = "VISUAL EFFECTS  (right controller)\n"
                        + "A  Quality: " + VfxSettings.QualityLabel + "\n"
                        + "B  Bloom: " + (VfxSettings.Bloom ? "ON" : "off") + "\n"
                        + "Applies to your next shot";
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
