using System.Collections;
using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
    public sealed class PixoVRTelemetryRunner : MonoBehaviour
    {
        private static PixoVRTelemetryRunner instance;
        private PixoVRAnalyticsProvider provider;

        public static void Start(PixoVRAnalyticsProvider analyticsProvider)
        {
            Ensure().provider = analyticsProvider;
        }

        public static void StartRequest(IEnumerator request)
        {
            Ensure().StartCoroutine(request);
        }

        private static PixoVRTelemetryRunner Ensure()
        {
            if (instance != null)
                return instance;

            GameObject runnerObject = new GameObject("PixoVRTelemetryRunner");
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(runnerObject);
            instance = runnerObject.AddComponent<PixoVRTelemetryRunner>();
            return instance;
        }

        private void Update()
        {
            provider?.Tick(Time.realtimeSinceStartupAsDouble);
        }

        private void OnApplicationQuit()
        {
            provider?.Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                provider?.Flush();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
