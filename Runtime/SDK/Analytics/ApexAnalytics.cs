using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public static class ApexAnalytics
    {
        private static readonly List<IApexAnalyticsProvider> providers = new();
        private static readonly List<ApexTrackedObject> trackedObjects = new();

        public static IReadOnlyList<IApexAnalyticsProvider> Providers => providers;
        public static IReadOnlyList<ApexTrackedObject> TrackedObjects => trackedObjects;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        public static bool Register(IApexAnalyticsProvider provider)
        {
            if (provider == null)
            {
                Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot register a null provider.");
                return false;
            }

            foreach (IApexAnalyticsProvider registeredProvider in providers)
            {
                if (ReferenceEquals(registeredProvider, provider) || registeredProvider.Name == provider.Name)
                {
                    Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", $"Provider '{provider.Name}' is already registered.");
                    return false;
                }
            }

            ApexAnalyticsSettings settings = ApexAnalyticsSettings.Load();
            if (settings != null)
            {
                if (!settings.AnalyticsEnabled)
                {
                    Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Analytics are disabled by settings.");
                    return false;
                }

                foreach (string disabledProvider in settings.DisabledProviders)
                {
                    if (disabledProvider == provider.Name)
                    {
                        Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", $"Provider '{provider.Name}' is disabled by settings.");
                        return false;
                    }
                }
            }

            providers.Add(provider);
            foreach (ApexTrackedObject trackedObject in trackedObjects.ToArray())
            {
                Invoke(provider, p => p.OnTrackedObjectRegistered(trackedObject));
            }

            return true;
        }

        public static bool Unregister(IApexAnalyticsProvider provider)
        {
            if (provider == null)
                return false;

            return providers.Remove(provider);
        }

        public static void Dispatch(Action<IApexAnalyticsProvider> call)
        {
            if (call == null)
                return;

            IApexAnalyticsProvider[] providerSnapshot = providers.ToArray();
            foreach (IApexAnalyticsProvider provider in providerSnapshot)
            {
                Invoke(provider, call);
            }
        }

        internal static void RegisterTrackedObject(ApexTrackedObject trackedObject)
        {
            if (trackedObject == null || trackedObjects.Contains(trackedObject))
                return;

            trackedObjects.Add(trackedObject);
            Dispatch(provider => provider.OnTrackedObjectRegistered(trackedObject));
        }

        internal static void UnregisterTrackedObject(ApexTrackedObject trackedObject)
        {
            if (trackedObject == null || !trackedObjects.Remove(trackedObject))
                return;

            Dispatch(provider => provider.OnTrackedObjectUnregistered(trackedObject));
        }

        internal static void Clear()
        {
            providers.Clear();
            trackedObjects.Clear();
        }

        private static void Invoke(
            IApexAnalyticsProvider provider,
            Action<IApexAnalyticsProvider> call)
        {
            try
            {
                call(provider);
            }
            catch (Exception ex)
            {
                Debug.unityLogger.Log(
                    LogType.Error,
                    "ApexAnalytics",
                    $"Provider '{provider.Name}' threw: {ex}");
            }
        }
    }
}
