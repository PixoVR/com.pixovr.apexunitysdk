using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public enum PixoVRTelemetrySinkType
    {
        None,
        Log,
        File,
        Http
    }

    [CreateAssetMenu(fileName = "ApexAnalyticsSettings", menuName = "PixoVR/Apex Analytics Settings")]
    public class ApexAnalyticsSettings : ScriptableObject
    {
        [SerializeField]
        private bool analyticsEnabled = true;

        [SerializeField]
        private List<string> disabledProviders = new();

        [SerializeField]
        private bool pixoVRProviderEnabled = true;

        [SerializeField]
        private PixoVRTelemetrySinkType telemetrySink = PixoVRTelemetrySinkType.Log;

        [SerializeField]
        private string telemetryHttpUrl;

        [SerializeField]
        private float flushIntervalSeconds = 10f;

        [SerializeField]
        private float poseSampleHz = 2f;

        [SerializeField]
        private int maxRecordsPerPacket = 500;

        public bool AnalyticsEnabled => analyticsEnabled;
        public IReadOnlyList<string> DisabledProviders => disabledProviders;
        public bool PixoVRProviderEnabled => pixoVRProviderEnabled;
        public PixoVRTelemetrySinkType TelemetrySink => telemetrySink;
        public string TelemetryHttpUrl => telemetryHttpUrl;
        public float FlushIntervalSeconds => flushIntervalSeconds;
        public float PoseSampleHz => poseSampleHz;
        public int MaxRecordsPerPacket => maxRecordsPerPacket;

        public static ApexAnalyticsSettings Load()
        {
            return Resources.Load<ApexAnalyticsSettings>("ApexAnalyticsSettings");
        }
    }
}
