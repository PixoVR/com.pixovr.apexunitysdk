using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	/// <summary>Where the built-in PixoVR provider's telemetry packets go.</summary>
	public enum ETelemetrySinkType
	{
		/// <summary>No telemetry is recorded.</summary>
		NONE,
		/// <summary>Each packet is written to the Unity log.</summary>
		LOG,
		/// <summary>Each packet is saved under persistentDataPath/ApexTelemetry.</summary>
		FILE,
		/// <summary>The core POSTs each packet to the telemetry URL with the user's token.</summary>
		HTTP,
	}

	[CreateAssetMenu(fileName = "ApexAnalyticsSettings", menuName = "PixoVR/Apex Analytics Settings")]
	public class ApexAnalyticsSettings : ScriptableObject
	{
		[SerializeField, FormerlySerializedAs("analyticsEnabled")]
		private bool m_analyticsEnabled = true;

		[SerializeField, FormerlySerializedAs("disabledProviders")]
		private List<string> m_disabledProviders = new List<string>();

		[SerializeField, FormerlySerializedAs("pixoVRProviderEnabled")]
		private bool m_pixoVRProviderEnabled = true;

		[SerializeField, FormerlySerializedAs("telemetrySink")]
		private ETelemetrySinkType m_telemetrySink = ETelemetrySinkType.LOG;

		[SerializeField, FormerlySerializedAs("telemetryHttpUrl")]
		private string m_telemetryHttpUrl;

		[SerializeField, FormerlySerializedAs("flushIntervalSeconds")]
		private float m_flushIntervalSeconds = 10f;

		[SerializeField, FormerlySerializedAs("poseSampleHz")]
		private float m_poseSamplesPerSecond = 2f;

		[SerializeField, FormerlySerializedAs("maxRecordsPerPacket")]
		private int m_maximumRecordsPerPacket = 500;

		public bool AnalyticsEnabled => m_analyticsEnabled;
		public IReadOnlyList<string> DisabledProviders => m_disabledProviders;
		public bool PixoVRProviderEnabled => m_pixoVRProviderEnabled;
		public ETelemetrySinkType TelemetrySink => m_telemetrySink;
		public string TelemetryHttpUrl => m_telemetryHttpUrl;
		public float FlushIntervalSeconds => m_flushIntervalSeconds;
		public float PoseSamplesPerSecond => m_poseSamplesPerSecond;
		public int MaximumRecordsPerPacket => m_maximumRecordsPerPacket;

		public static ApexAnalyticsSettings Load()
		{
			return Resources.Load<ApexAnalyticsSettings>("ApexAnalyticsSettings");
		}

		/// <summary>True when the PixoVR provider may run: analytics on, provider on, not disabled.</summary>
		public static bool IsPixoVRProviderEnabled(ApexAnalyticsSettings settings)
		{
			if (settings == null)
			{
				return true;
			}

			bool disabledByName = false;
			foreach (string disabledProvider in settings.DisabledProviders)
			{
				if (disabledProvider == PixoVR.PixoVRAnalyticsProvider.kProviderName)
				{
					disabledByName = true;
				}
			}

			return settings.AnalyticsEnabled && settings.PixoVRProviderEnabled && !disabledByName
				&& settings.TelemetrySink != ETelemetrySinkType.NONE;
		}
	}
}
