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

        /// <summary>
        /// Gets the per-instance id. Authored scene objects keep their serialized id across runs;
        /// spawned or duplicated instances get a fresh id when enabled.
        /// </summary>
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
            EnsureUniqueTrackedId();
            ApexAnalytics.RegisterTrackedObject(this);
        }

        void OnDisable()
        {
            ApexAnalytics.UnregisterTrackedObject(this);
        }

        public void SetTrackedId(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Tracked id cannot be null or empty.", nameof(id));

            bool wasRegistered = isActiveAndEnabled && IsRegistered();
            if (wasRegistered)
            {
                ApexAnalytics.UnregisterTrackedObject(this);
            }

            trackedId = id;

            if (wasRegistered)
            {
                ApexAnalytics.RegisterTrackedObject(this);
            }
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

        private void EnsureUniqueTrackedId()
        {
            EnsureTrackedId();

            foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
            {
                if (trackedObject != this && trackedObject.TrackedId == trackedId)
                {
                    trackedId = Guid.NewGuid().ToString("N");
                    break;
                }
            }
        }

        private bool IsRegistered()
        {
            foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
            {
                if (trackedObject == this)
                    return true;
            }

            return false;
        }
    }
}
