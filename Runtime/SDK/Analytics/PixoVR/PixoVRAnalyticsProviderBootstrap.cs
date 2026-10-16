using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
	/// <summary>Registers the PixoVR provider at startup unless the ApexAnalyticsSettings turn it off.</summary>
	public static class PixoVRAnalyticsProviderBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AutoRegister()
		{
			if (!ApexAnalyticsSettings.IsPixoVRProviderEnabled(ApexAnalyticsSettings.Load()))
			{
				return;
			}

			PixoVRAnalyticsProvider provider = new PixoVRAnalyticsProvider();
			if (ApexAnalytics.Register(provider))
			{
				PixoVRTelemetryRunner.Start(provider);
			}
		}
	}
}
