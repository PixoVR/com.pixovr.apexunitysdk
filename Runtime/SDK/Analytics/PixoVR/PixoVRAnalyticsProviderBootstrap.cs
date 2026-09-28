using System;
using System.IO;
using PixoVR.Apex;
using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
    public static class PixoVRAnalyticsProviderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRegister()
        {
            ApexAnalyticsSettings settings = ApexAnalyticsSettings.Load();
            if (settings != null && !settings.PixoVRProviderEnabled)
                return;

            PixoVRTelemetrySinkType sinkType = settings == null
                ? PixoVRTelemetrySinkType.Log
                : settings.TelemetrySink;
            if (sinkType == PixoVRTelemetrySinkType.None)
                return;

            IApexTelemetrySink sink = CreateSink(settings, sinkType);
            PixoVRProviderOptions options = new PixoVRProviderOptions
            {
                FlushIntervalSeconds = settings == null ? 10f : settings.FlushIntervalSeconds,
                PoseSampleHz = settings == null ? 2f : settings.PoseSampleHz,
                MaxRecordsPerPacket = settings == null ? 500 : settings.MaxRecordsPerPacket
            };
            ApexAnalytics.Register(new PixoVRAnalyticsProvider(sink, options));
        }

        private static IApexTelemetrySink CreateSink(
            ApexAnalyticsSettings settings,
            PixoVRTelemetrySinkType sinkType)
        {
            switch (sinkType)
            {
                case PixoVRTelemetrySinkType.File:
                    return new FileTelemetrySink(Path.Combine(Application.persistentDataPath, "ApexTelemetry"));
                case PixoVRTelemetrySinkType.Http:
                    if (settings == null || string.IsNullOrEmpty(settings.TelemetryHttpUrl))
                    {
                        Debug.LogWarning("[PixoVRAnalytics] HTTP sink requires a URL; using log sink.");
                        return new LogTelemetrySink();
                    }

                    return new HttpTelemetrySink(
                        settings.TelemetryHttpUrl,
                        () => ApexSystem.CurrentUser?.Token);
                default:
                    return new LogTelemetrySink();
            }
        }
    }
}
