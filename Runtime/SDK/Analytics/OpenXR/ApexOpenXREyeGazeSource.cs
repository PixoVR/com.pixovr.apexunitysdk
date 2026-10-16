#if APEX_OPENXR
using UnityEngine;
using UnityEngine.Serialization;
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
		[SerializeField, FormerlySerializedAs("trackingOrigin")]
		private Transform m_trackingOrigin;

		[SerializeField, FormerlySerializedAs("fallbackToHead")]
		private bool m_fallbackToHead = true;

		[SerializeField, FormerlySerializedAs("engagementName")]
		private string m_engagementName = "eye_gaze";

		public override bool TryGetGaze(out Ray ray, out string engagement)
		{
			EyeGazeInteraction.EyeGazeDevice device =
				InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
			if (device != null && device.pose.isTracked.isPressed)
			{
				Vector3 position = device.pose.position.ReadValue();
				Quaternion rotation = device.pose.rotation.ReadValue();
				if (m_trackingOrigin != null)
				{
					position = m_trackingOrigin.TransformPoint(position);
					rotation = m_trackingOrigin.rotation * rotation;
				}

				ray = new Ray(position, rotation * Vector3.forward);
				engagement = m_engagementName;
				return true;
			}

			if (m_fallbackToHead)
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
