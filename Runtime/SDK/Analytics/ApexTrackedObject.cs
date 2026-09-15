using System;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    [DisallowMultipleComponent]
    [AddComponentMenu("PixoVR/Apex Tracked Object")]
    public class ApexTrackedObject : MonoBehaviour
    {
        [SerializeField]
        private string trackedId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private string meshName;

        public string TrackedId => trackedId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        public string MeshName => meshName;

        void Awake()
        {
            EnsureTrackedId();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            EnsureTrackedId();
        }
#endif

        void OnEnable()
        {
            ApexAnalytics.Dispatch(provider => provider.OnTrackedObjectRegistered(this));
        }

        void OnDisable()
        {
            ApexAnalytics.Dispatch(provider => provider.OnTrackedObjectUnregistered(this));
        }

        public void BeginEngagement(string engagement)
        {
            if (string.IsNullOrEmpty(engagement))
            {
                Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot begin an empty engagement.");
                return;
            }

            ApexAnalytics.Dispatch(provider => provider.OnEngagementBegin(this, engagement));
        }

        public void EndEngagement(string engagement)
        {
            if (string.IsNullOrEmpty(engagement))
            {
                Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot end an empty engagement.");
                return;
            }

            ApexAnalytics.Dispatch(provider => provider.OnEngagementEnd(this, engagement));
        }

        private void EnsureTrackedId()
        {
            if (string.IsNullOrEmpty(trackedId))
            {
                trackedId = Guid.NewGuid().ToString("N");
            }
        }
    }
}
