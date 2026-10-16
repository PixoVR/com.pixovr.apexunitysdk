using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
	/// <summary>The registered analytics providers and tracked objects, and the dispatch between them.</summary>
	public static class ApexAnalytics
	{
		private const string kLogTag = "ApexAnalytics";

		private static readonly List<IApexAnalyticsProvider> kProviders = new List<IApexAnalyticsProvider>();
		private static readonly List<ApexTrackedObject> kTrackedObjects = new List<ApexTrackedObject>();

		public static IReadOnlyList<IApexAnalyticsProvider> Providers => kProviders;
		public static IReadOnlyList<ApexTrackedObject> TrackedObjects => kTrackedObjects;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			Clear();
		}

		public static bool Register(IApexAnalyticsProvider provider)
		{
			if (provider == null)
			{
				Debug.unityLogger.Log(LogType.Warning, kLogTag, "Cannot register a null provider.");
				return false;
			}

			foreach (IApexAnalyticsProvider registeredProvider in kProviders)
			{
				if (ReferenceEquals(registeredProvider, provider) || registeredProvider.Name == provider.Name)
				{
					Debug.unityLogger.Log(LogType.Warning, kLogTag,
						$"Provider '{provider.Name}' is already registered.");
					return false;
				}
			}

			ApexAnalyticsSettings settings = ApexAnalyticsSettings.Load();
			if (settings != null)
			{
				if (!settings.AnalyticsEnabled)
				{
					Debug.unityLogger.Log(LogType.Warning, kLogTag, "Analytics are disabled by settings.");
					return false;
				}

				foreach (string disabledProvider in settings.DisabledProviders)
				{
					if (disabledProvider == provider.Name)
					{
						Debug.unityLogger.Log(LogType.Warning, kLogTag,
							$"Provider '{provider.Name}' is disabled by settings.");
						return false;
					}
				}
			}

			kProviders.Add(provider);
			foreach (ApexTrackedObject trackedObject in kTrackedObjects.ToArray())
			{
				Invoke(provider, registeredProvider => registeredProvider.OnTrackedObjectRegistered(trackedObject));
			}

			return true;
		}

		public static bool Unregister(IApexAnalyticsProvider provider)
		{
			if (provider == null)
			{
				return false;
			}

			return kProviders.Remove(provider);
		}

		public static void Dispatch(Action<IApexAnalyticsProvider> providerCall)
		{
			if (providerCall == null)
			{
				return;
			}

			foreach (IApexAnalyticsProvider provider in kProviders.ToArray())
			{
				Invoke(provider, providerCall);
			}
		}

		internal static void RegisterTrackedObject(ApexTrackedObject trackedObject)
		{
			if (trackedObject == null || kTrackedObjects.Contains(trackedObject))
			{
				return;
			}

			kTrackedObjects.Add(trackedObject);
			Dispatch(provider => provider.OnTrackedObjectRegistered(trackedObject));
		}

		internal static void UnregisterTrackedObject(ApexTrackedObject trackedObject)
		{
			if (trackedObject == null || !kTrackedObjects.Remove(trackedObject))
			{
				return;
			}

			Dispatch(provider => provider.OnTrackedObjectUnregistered(trackedObject));
		}

		internal static void Clear()
		{
			kProviders.Clear();
			kTrackedObjects.Clear();
		}

		private static void Invoke(IApexAnalyticsProvider provider, Action<IApexAnalyticsProvider> providerCall)
		{
			try
			{
				providerCall(provider);
			}
			catch (Exception exception)
			{
				Debug.unityLogger.Log(LogType.Error, kLogTag, $"Provider '{provider.Name}' threw: {exception}");
			}
		}
	}
}
