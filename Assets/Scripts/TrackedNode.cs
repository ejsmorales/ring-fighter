using UnityEngine;
using UnityEngine.XR;

namespace RingFighter
{
    /// <summary>
    /// Drives this transform from an XR device (the headset or a hand controller) and
    /// exposes that device's input (trigger, grip, buttons) plus a short position history
    /// used for gesture recognition. Put it on the camera (Head) and on each hand object.
    /// The parent transform is the rig root, so transform.position is world space.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // update tracking before anything that reads it
    public class TrackedNode : MonoBehaviour
    {
        public enum NodeType { Head, LeftHand, RightHand }

        public NodeType node = NodeType.Head;

        [Tooltip("Optional child transform whose forward is the 'pointing' direction. Rotate it to calibrate aim.")]
        public Transform aim;

        [Tooltip("Trigger value (0-1) above which the trigger counts as held.")]
        public float triggerThreshold = 0.6f;

        // ---- Live state ----
        public bool IsTracked { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float Trigger { get; private set; }
        public float Grip { get; private set; }
        public bool PrimaryButton { get; private set; }
        public bool SecondaryButton { get; private set; }
        public bool TriggerHeld { get { return Trigger >= triggerThreshold; } }
        public bool IsHand { get { return node != NodeType.Head; } }
        public int HandMask { get { return node == NodeType.LeftHand ? 1 : (node == NodeType.RightHand ? 2 : 0); } }

        public Vector3 AimForward
        {
            get { return aim != null ? aim.forward : transform.forward; }
        }

        public Vector3 AimOrigin
        {
            get { return aim != null ? aim.position : transform.position; }
        }

        // ---- Position history (ring buffer) ----
        private const int HistorySize = 160;
        private readonly Vector3[] historyPos = new Vector3[HistorySize];
        private readonly float[] historyTime = new float[HistorySize];
        private int historyHead;   // index of the next write
        private int historyCount;

        private InputDevice device;
        private InputDevice fallbackDevice;
        private Vector3 lastPos;
        private bool hasLastPos;

        private XRNode PrimaryXRNode
        {
            get
            {
                switch (node)
                {
                    case NodeType.LeftHand: return XRNode.LeftHand;
                    case NodeType.RightHand: return XRNode.RightHand;
                    default: return XRNode.Head;
                }
            }
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void Update()
        {
            Poll(true);
        }

        // Re-read the pose just before the frame is drawn, so the view and hands use the freshest
        // tracking data. Skipping this adds a frame of lag, which feels swimmy in VR.
        private void OnBeforeRender()
        {
            Poll(false);
        }

        private InputDevice FindSource()
        {
            if (!device.isValid)
                device = InputDevices.GetDeviceAtXRNode(PrimaryXRNode);
            if (!device.isValid && node == NodeType.Head && !fallbackDevice.isValid)
                fallbackDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            return device.isValid ? device : fallbackDevice;
        }

        private void Poll(bool fullUpdate)
        {
            InputDevice source = FindSource();
            if (!source.isValid)
            {
                IsTracked = false;
                return;
            }

            Vector3 pos;
            Quaternion rot;
            bool gotPos = source.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            bool gotRot = source.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
            if (!gotPos && node == NodeType.Head)
                gotPos = source.TryGetFeatureValue(CommonUsages.centerEyePosition, out pos);
            if (!gotRot && node == NodeType.Head)
                gotRot = source.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rot);

            bool tracked;
            if (!source.TryGetFeatureValue(CommonUsages.isTracked, out tracked))
                tracked = true;

            bool isTracked = tracked && gotPos && gotRot;
            if (isTracked)
            {
                transform.localPosition = pos;
                transform.localRotation = rot;
            }
            if (!fullUpdate) return;   // pre-render pass: pose only

            IsTracked = isTracked;
            if (IsTracked)
                RecordSample(transform.position);

            if (IsHand)
            {
                float v;
                Trigger = source.TryGetFeatureValue(CommonUsages.trigger, out v) ? v : 0f;
                Grip = source.TryGetFeatureValue(CommonUsages.grip, out v) ? v : 0f;
                bool b;
                PrimaryButton = source.TryGetFeatureValue(CommonUsages.primaryButton, out b) && b;
                SecondaryButton = source.TryGetFeatureValue(CommonUsages.secondaryButton, out b) && b;
            }
        }

        private void RecordSample(Vector3 worldPos)
        {
            float dt = Time.deltaTime;
            if (hasLastPos && dt > 0.0001f)
                Velocity = (worldPos - lastPos) / dt;
            lastPos = worldPos;
            hasLastPos = true;

            historyPos[historyHead] = worldPos;
            historyTime[historyHead] = Time.time;
            historyHead = (historyHead + 1) % HistorySize;
            if (historyCount < HistorySize) historyCount++;
        }

        /// <summary>
        /// Copies the positions recorded during the last 'seconds' (oldest first) into buffer.
        /// Returns how many were copied.
        /// </summary>
        public int CopyHistory(float seconds, Vector3[] buffer)
        {
            float cutoff = Time.time - seconds;
            int n = 0;
            int start = (historyHead - historyCount + HistorySize) % HistorySize;
            for (int i = 0; i < historyCount && n < buffer.Length; i++)
            {
                int idx = (start + i) % HistorySize;
                if (historyTime[idx] >= cutoff)
                    buffer[n++] = historyPos[idx];
            }
            return n;
        }

        /// <summary>Short vibration on this controller. Does nothing on the headset.</summary>
        public void Haptic(float amplitude, float duration)
        {
            if (!IsHand || !device.isValid) return;
            HapticCapabilities caps;
            if (device.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }
    }
}
