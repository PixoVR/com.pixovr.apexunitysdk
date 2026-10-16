using Newtonsoft.Json.Linq;

namespace PixoVR.Apex.Analytics
{
	/// <summary>
	/// Receives what the player does. Register one with <see cref="ApexAnalytics.Register"/>; every
	/// method has an empty default, so a provider implements only what it needs.
	/// </summary>
	public interface IApexAnalyticsProvider
	{
		string Name { get; }

		void OnUserIdentified(ApexAnalyticsContext context, LoginResponseContent user)
		{
		}

		void OnSessionJoined(ApexAnalyticsContext context, JoinSessionResponse session)
		{
		}

		/// <summary>eventData is the module's own data sent with the event, or null.</summary>
		void OnSessionEvent(ApexAnalyticsContext context, string action, string targetObject, JObject eventData)
		{
		}

		void OnSessionCompleted(ApexAnalyticsContext context, SessionData sessionData)
		{
		}

		void OnTrackedObjectRegistered(ApexTrackedObject trackedObject)
		{
		}

		void OnTrackedObjectUnregistered(ApexTrackedObject trackedObject)
		{
		}

		void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement)
		{
		}

		void OnEngagementEnd(ApexTrackedObject trackedObject, string engagement)
		{
		}

		void OnInteraction(ApexTrackedObject trackedObject, string action)
		{
		}

		void OnStepBegin(ApexAnalyticsContext context, string step)
		{
		}

		void OnStepEnd(ApexAnalyticsContext context, string step, bool success, float score)
		{
		}

		void Flush()
		{
		}
	}
}
