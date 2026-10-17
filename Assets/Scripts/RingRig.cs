using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RingFighter
{
    /// <summary>
    /// Root of the player: holds the head and both hands and makes sure the headset
    /// reports positions relative to the floor (so the camera sits at real standing height).
    /// </summary>
    public class RingRig : MonoBehaviour
    {
        public TrackedNode head;
        public TrackedNode leftHand;
        public TrackedNode rightHand;

        private readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        private float originRetryUntil;
        private bool originSet;

        private void Start()
        {
            originRetryUntil = Time.time + 8f;
            TrySetFloorOrigin();
        }

        private bool warnedNoXR;

        private void Update()
        {
            // The XR system may start a moment after the scene does, so retry for a few seconds.
            if (!originSet && Time.time < originRetryUntil)
                TrySetFloorOrigin();

            if (!warnedNoXR && Time.time > 5f && !XRSettings.isDeviceActive)
            {
                warnedNoXR = true;
                Debug.LogError("Ring Fighter: VR is not running, so head and hand tracking are off. " +
                    "Check Project Settings > XR Plug-in Management: OpenXR must be ticked for this platform.");
            }
        }

        private void TrySetFloorOrigin()
        {
            SubsystemManager.GetSubsystems(subsystems);
            for (int i = 0; i < subsystems.Count; i++)
            {
                if (subsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    originSet = true;
            }
        }

        public TrackedNode GetHand(int mask)
        {
            return mask == 1 ? leftHand : rightHand;
        }

        /// <summary>Direction the player is facing, flattened onto the ground plane.</summary>
        public Vector3 FlatForward()
        {
            Vector3 f = head != null ? head.transform.forward : transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) f = transform.forward;
            return f.normalized;
        }
    }
}
