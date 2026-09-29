using PixoVR.Apex;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    [AddComponentMenu("PixoVR/Apex Interactable")]
    public class ApexInteractable : ApexTrackedObject
    {
        [SerializeField]
        private bool sendSessionEvents;

        public bool IsGrabbed { get; private set; }
        public bool IsHovered { get; private set; }

        public void Grab()
        {
            if (IsGrabbed)
                return;

            IsGrabbed = true;
            BeginEngagement("grab");
            SendSessionEvent("grab");
        }

        public void Release()
        {
            if (!IsGrabbed)
                return;

            IsGrabbed = false;
            EndEngagement("grab");
            SendSessionEvent("release");
        }

        public void HoverEnter()
        {
            if (IsHovered)
                return;

            IsHovered = true;
            BeginEngagement("hover");
        }

        public void HoverExit()
        {
            if (!IsHovered)
                return;

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
            if (!sendSessionEvents || FindObjectOfType<ApexSystem>() == null)
                return;

            ApexSystem.SendSimpleSessionEvent(action, DisplayName, null);
        }
    }
}
