using PixoVR.Apex;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	[AddComponentMenu("PixoVR/Apex Interactable")]
	public class ApexInteractable : ApexTrackedObject
	{
		[SerializeField, FormerlySerializedAs("sendSessionEvents")]
		private bool m_sendSessionEvents;

		public bool IsGrabbed { get; private set; }
		public bool IsHovered { get; private set; }

		public void Grab()
		{
			if (IsGrabbed)
			{
				return;
			}

			IsGrabbed = true;
			BeginEngagement("grab");
			SendSessionEvent("grab");
		}

		public void Release()
		{
			if (!IsGrabbed)
			{
				return;
			}

			IsGrabbed = false;
			EndEngagement("grab");
			SendSessionEvent("release");
		}

		public void HoverEnter()
		{
			if (IsHovered)
			{
				return;
			}

			IsHovered = true;
			BeginEngagement("hover");
		}

		public void HoverExit()
		{
			if (!IsHovered)
			{
				return;
			}

			IsHovered = false;
			EndEngagement("hover");
		}

		public void Use()
		{
			RecordInteraction("use");
			SendSessionEvent("use");
		}

		private void SendSessionEvent(string action)
		{
			if (!m_sendSessionEvents || FindObjectOfType<ApexSystem>() == null)
			{
				return;
			}

			ApexSystem.SendSessionEvent(action, DisplayName);
		}
	}
}
