using System;
using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
	/// <summary>
	/// The built-in provider: forwards what the player does to the Apex core, which records it in
	/// the PixoVR telemetry format, batches it into packets and sends them where the
	/// ApexAnalyticsSettings say. Poses are sampled when the core asks for them.
	/// </summary>
	public sealed class PixoVRAnalyticsProvider : IApexAnalyticsProvider
	{
		public const string kProviderName = "PixoVR";

		private readonly Func<IApexTelemetryRecorder> m_recorderSource;

		public PixoVRAnalyticsProvider()
			: this(ApexSystem.FindTelemetryRecorder)
		{
		}

		/// <param name="recorderSource">Returns the recorder to use now, or null when none is running.</param>
		internal PixoVRAnalyticsProvider(Func<IApexTelemetryRecorder> recorderSource)
		{
			m_recorderSource = recorderSource ?? ApexSystem.FindTelemetryRecorder;
		}

		public string Name => kProviderName;

		public void OnTrackedObjectRegistered(ApexTrackedObject trackedObject)
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder == null || trackedObject == null)
			{
				return;
			}

			Transform objectTransform = trackedObject.transform;
			recorder.TelemetryObjectRegistered(trackedObject.TrackedId, trackedObject.DisplayName,
				trackedObject.MeshName, objectTransform.position, objectTransform.eulerAngles);
		}

		public void OnTrackedObjectUnregistered(ApexTrackedObject trackedObject)
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder == null || trackedObject == null)
			{
				return;
			}

			Transform objectTransform = trackedObject.transform;
			recorder.TelemetryObjectUnregistered(trackedObject.TrackedId, objectTransform.position,
				objectTransform.eulerAngles);
		}

		public void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement)
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder != null && trackedObject != null)
			{
				recorder.TelemetryEngagementBegin(trackedObject.TrackedId, engagement);
			}
		}

		public void OnEngagementEnd(ApexTrackedObject trackedObject, string engagement)
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder != null && trackedObject != null)
			{
				recorder.TelemetryEngagementEnd(trackedObject.TrackedId, engagement);
			}
		}

		public void OnInteraction(ApexTrackedObject trackedObject, string action)
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder != null && trackedObject != null)
			{
				recorder.TelemetryInteraction(trackedObject.TrackedId, action);
			}
		}

		public void OnStepBegin(ApexAnalyticsContext context, string step)
		{
			m_recorderSource()?.TelemetryStepBegin(step);
		}

		public void OnStepEnd(ApexAnalyticsContext context, string step, bool success, float score)
		{
			m_recorderSource()?.TelemetryStepEnd(step, success, score);
		}

		public void Flush()
		{
			m_recorderSource()?.TelemetryFlush();
		}

		/// <summary>Reports the head and every pose-tracked object when the core asks for a sample.</summary>
		public void SamplePoses()
		{
			IApexTelemetryRecorder recorder = m_recorderSource();
			if (recorder == null || !recorder.TelemetryPoseDue())
			{
				return;
			}

			Transform head = ApexSpatialSampler.Head;
			if (head == null && Camera.main != null)
			{
				head = Camera.main.transform;
			}

			if (head != null)
			{
				recorder.TelemetryPose(null, head.position, head.eulerAngles);
			}

			foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
			{
				if (trackedObject != null && trackedObject.TrackPose)
				{
					Transform objectTransform = trackedObject.transform;
					recorder.TelemetryPose(trackedObject.TrackedId, objectTransform.position,
						objectTransform.eulerAngles);
				}
			}
		}
	}
}
