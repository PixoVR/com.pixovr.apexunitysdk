using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
	/// <summary>Samples poses for the PixoVR provider every frame, and flushes when the app pauses.</summary>
	public sealed class PixoVRTelemetryRunner : MonoBehaviour
	{
		private static PixoVRTelemetryRunner m_instance;
		private PixoVRAnalyticsProvider m_provider;

		public static void Start(PixoVRAnalyticsProvider analyticsProvider)
		{
			Ensure().m_provider = analyticsProvider;
		}

		private static PixoVRTelemetryRunner Ensure()
		{
			if (m_instance != null)
			{
				return m_instance;
			}

			GameObject runnerObject = new GameObject("PixoVRTelemetryRunner");
			runnerObject.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(runnerObject);
			m_instance = runnerObject.AddComponent<PixoVRTelemetryRunner>();
			return m_instance;
		}

		private void Update()
		{
			m_provider?.SamplePoses();
		}

		private void OnApplicationPause(bool paused)
		{
			if (paused)
			{
				m_provider?.Flush();
			}
		}

		private void OnDestroy()
		{
			if (m_instance == this)
			{
				m_instance = null;
			}
		}
	}
}
