#if APEX_OPENXR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace PixoVR.Apex.Analytics.OpenXR
{
    /// <summary>
    /// Reads OpenXR eye gaze when EyeGazeInteraction is enabled; untested without a headset in CI.
    /// </summary>
    public sealed class ApexOpenXREyeGazeSource : ApexGazeSource
    {
        [SerializeField]
        private Transform trackingOrigin;

        [SerializeField]
        private bool fallbackToHead = true;

        [SerializeField]
        private string engagementName = "eye_gaze";

        public override bool TryGetGaze(out Ray ray, out string engagement)
        {
            EyeGazeInteraction.EyeGazeDevice device =
                InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
            if (device != null && device.pose.isTracked.isPressed)
            {
                Vector3 position = device.pose.position.ReadValue();
                Quaternion rotation = device.pose.rotation.ReadValue();
                if (trackingOrigin != null)
                {
                    position = trackingOrigin.TransformPoint(position);
                    rotation = trackingOrigin.rotation * rotation;
                }

                ray = new Ray(position, rotation * Vector3.forward);
                engagement = engagementName;
                return true;
            }

            if (fallbackToHead)
            {
                Transform head = ApexSpatialSampler.Head != null
                    ? ApexSpatialSampler.Head
                    : transform;
                ray = new Ray(head.position, head.forward);
                engagement = "gaze";
                return true;
            }

            ray = default;
            engagement = null;
            return false;
        }
    }
}
#endif
