using PixoVR.Apex;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public static class ApexSteps
    {
        public static void BeginStep(string step)
        {
            ApexAnalyticsContext context;
            if (!TryGetContext(step, "begin", out context))
                return;

            ApexAnalytics.Dispatch(provider => provider.OnStepBegin(context, step));
        }

        public static void CompleteStep(string step, float score = 1f)
        {
            ApexAnalyticsContext context;
            if (!TryGetContext(step, "complete", out context))
                return;

            ApexAnalytics.Dispatch(provider => provider.OnStepEnd(context, step, true, score));
        }

        public static void FailStep(string step, float score = 0f)
        {
            ApexAnalyticsContext context;
            if (!TryGetContext(step, "fail", out context))
                return;

            ApexAnalytics.Dispatch(provider => provider.OnStepEnd(context, step, false, score));
        }

        private static bool TryGetContext(
            string step,
            string action,
            out ApexAnalyticsContext context)
        {
            if (string.IsNullOrEmpty(step))
            {
                Debug.unityLogger.Log(
                    LogType.Warning,
                    "ApexAnalytics",
                    "Cannot " + action + " an empty step.");
                context = null;
                return false;
            }

            ApexSystem system = Object.FindObjectOfType<ApexSystem>();
            if (system == null)
            {
                context = null;
                return false;
            }

            context = ApexSystem.AnalyticsContext;
            return true;
        }
    }
}
