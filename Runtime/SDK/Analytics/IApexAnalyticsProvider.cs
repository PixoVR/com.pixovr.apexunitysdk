using PixoVR.Apex;
using PixoVR.Apex.XAPI;
using TinCan;

namespace PixoVR.Apex.Analytics
{
    public interface IApexAnalyticsProvider
    {
        string Name { get; }

        void OnUserIdentified(ApexAnalyticsContext context, LoginResponseContent user) { }
        void OnSessionJoined(ApexAnalyticsContext context, JoinSessionResponse session) { }
        void OnSessionEvent(ApexAnalyticsContext context, Statement statement) { }
        void OnSessionCompleted(ApexAnalyticsContext context, SessionData data) { }
        void OnTrackedObjectRegistered(ApexTrackedObject trackedObject) { }
        void OnTrackedObjectUnregistered(ApexTrackedObject trackedObject) { }
        void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement) { }
        void OnEngagementEnd(ApexTrackedObject trackedObject, string engagement) { }
        void Flush() { }
    }
}
