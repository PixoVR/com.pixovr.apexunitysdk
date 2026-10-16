using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PixoVR.Apex.Analytics;
using PixoVR.Apex.Analytics.PixoVR;
using UnityEngine;

namespace PixoVR.Apex.Tests
{
	/// <summary>
	/// The PixoVR provider forwards to the core; these tests check what it forwards. The core's own
	/// tests cover how records become packets.
	/// </summary>
	public class PixoVRAnalyticsProviderTests
	{
		private readonly List<GameObject> m_createdObjects = new List<GameObject>();

		[SetUp]
		public void SetUp()
		{
			ApexAnalytics.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (GameObject createdObject in m_createdObjects)
			{
				if (createdObject != null)
				{
					Object.DestroyImmediate(createdObject);
				}
			}

			ApexAnalytics.Clear();
		}

		[Test]
		public void RegistrationForwardsIdNameMeshAndPose()
		{
			RecordingRecorder recorder = new RecordingRecorder();
			PixoVRAnalyticsProvider provider = new PixoVRAnalyticsProvider(() => recorder);
			GameObject trackedGameObject = CreateObject("Lever");
			trackedGameObject.transform.position = new Vector3(1.234f, 0f, -2.01f);
			ApexTrackedObject trackedObject = trackedGameObject.AddComponent<ApexTrackedObject>();

			provider.OnTrackedObjectRegistered(trackedObject);

			Assert.That(recorder.Calls, Is.EqualTo(new[] { "registered:" + trackedObject.TrackedId + ":Lever" }));
			Assert.That(recorder.LastPosition, Is.EqualTo(new Vector3(1.234f, 0f, -2.01f)));
		}

		[Test]
		public void EngagementInteractionAndStepsAreForwarded()
		{
			RecordingRecorder recorder = new RecordingRecorder();
			PixoVRAnalyticsProvider provider = new PixoVRAnalyticsProvider(() => recorder);
			ApexTrackedObject trackedObject = CreateObject("Tracked").AddComponent<ApexTrackedObject>();
			string trackedId = trackedObject.TrackedId;

			provider.OnEngagementBegin(trackedObject, "grab");
			provider.OnEngagementEnd(trackedObject, "grab");
			provider.OnInteraction(trackedObject, "use");
			provider.OnStepBegin(null, "step");
			provider.OnStepEnd(null, "step", true, 0.75f);
			provider.Flush();

			Assert.That(recorder.Calls, Is.EqualTo(new[]
			{
				"engagement-begin:" + trackedId + ":grab",
				"engagement-end:" + trackedId + ":grab",
				"interaction:" + trackedId + ":use",
				"step-begin:step",
				"step-end:step:True:0.75",
				"flush",
			}));
		}

		[Test]
		public void NothingIsForwardedWithoutARecorder()
		{
			PixoVRAnalyticsProvider provider = new PixoVRAnalyticsProvider(() => null);
			ApexTrackedObject trackedObject = CreateObject("Tracked").AddComponent<ApexTrackedObject>();

			Assert.DoesNotThrow(() =>
			{
				provider.OnTrackedObjectRegistered(trackedObject);
				provider.OnInteraction(trackedObject, "use");
				provider.OnStepBegin(null, "step");
				provider.SamplePoses();
				provider.Flush();
			});
		}

		[Test]
		public void PosesAreSampledOnlyWhenTheCoreAsks()
		{
			RecordingRecorder recorder = new RecordingRecorder();
			PixoVRAnalyticsProvider provider = new PixoVRAnalyticsProvider(() => recorder);
			GameObject headObject = CreateObject("Head");
			headObject.AddComponent<ApexSpatialSampler>();
			GameObject posedObject = CreateObject("Posed");
			ApexTrackedObject posedTrackedObject = posedObject.AddComponent<ApexTrackedObject>();
			SetPrivateField(posedTrackedObject, "m_trackPose", true);
			CreateObject("Unposed").AddComponent<ApexTrackedObject>();

			recorder.PoseDue = false;
			provider.SamplePoses();
			Assert.That(recorder.Calls, Is.Empty);

			recorder.PoseDue = true;
			provider.SamplePoses();
			Assert.That(recorder.Calls, Is.EqualTo(new[] { "pose:", "pose:" + posedTrackedObject.TrackedId }));
		}

		[Test]
		public void ApexInteractableIgnoresDoubleGrab()
		{
			RecordingRecorder recorder = new RecordingRecorder();
			ApexAnalytics.Register(new PixoVRAnalyticsProvider(() => recorder));
			ApexInteractable interactable = CreateObject("Interactable").AddComponent<ApexInteractable>();
			recorder.Calls.Clear();

			interactable.Grab();
			interactable.Grab();

			Assert.That(recorder.Calls.FindAll(call => call.StartsWith("engagement-begin:")), Has.Count.EqualTo(1));
		}

		[Test]
		public void GazeSourceBeginsAndEndsConfiguredEngagement()
		{
			RecordingProvider provider = new RecordingProvider("gaze");
			ApexAnalytics.Register(provider);
			GameObject sourceObject = CreateObject("Gaze");
			FakeGazeSource gazeSource = sourceObject.AddComponent<FakeGazeSource>();
			ApexGazeTracker gazeTracker = sourceObject.AddComponent<ApexGazeTracker>();
			SetPrivateField(gazeTracker, "m_dwellSeconds", 0f);

			GameObject targetObject = CreateObject("Target");
			targetObject.transform.position = new Vector3(0f, 0f, 3f);
			targetObject.AddComponent<BoxCollider>();
			ApexTrackedObject target = targetObject.AddComponent<ApexTrackedObject>();
			gazeSource.SetGaze(true, new Ray(Vector3.zero, Vector3.forward), "eye_gaze");
			Physics.SyncTransforms();

			gazeTracker.Sample();
			gazeTracker.Sample();
			Assert.That(provider.Calls, Does.Contain("begin:eye_gaze"));

			gazeSource.SetGaze(false, default, null);
			gazeTracker.Sample();
			Assert.That(provider.Calls, Does.Contain("end:eye_gaze"));
			Assert.That(provider.LastTrackedObject, Is.SameAs(target));
		}

		private GameObject CreateObject(string objectName)
		{
			GameObject createdObject = new GameObject(objectName);
			m_createdObjects.Add(createdObject);
			return createdObject;
		}

		private static void SetPrivateField(object target, string fieldName, object fieldValue)
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(target, fieldValue);
		}

		private sealed class RecordingRecorder : IApexTelemetryRecorder
		{
			public List<string> Calls { get; } = new List<string>();
			public Vector3 LastPosition { get; private set; }
			public bool PoseDue { get; set; }

			public void TelemetryObjectRegistered(string trackedId, string objectName, string meshName,
				Vector3 position, Vector3 rotation)
			{
				LastPosition = position;
				Calls.Add("registered:" + trackedId + ":" + objectName);
			}

			public void TelemetryObjectUnregistered(string trackedId, Vector3 position, Vector3 rotation)
			{
				Calls.Add("unregistered:" + trackedId);
			}

			public void TelemetryEngagementBegin(string trackedId, string engagementName)
			{
				Calls.Add("engagement-begin:" + trackedId + ":" + engagementName);
			}

			public void TelemetryEngagementEnd(string trackedId, string engagementName)
			{
				Calls.Add("engagement-end:" + trackedId + ":" + engagementName);
			}

			public void TelemetryInteraction(string trackedId, string actionName)
			{
				Calls.Add("interaction:" + trackedId + ":" + actionName);
			}

			public void TelemetryStepBegin(string stepName)
			{
				Calls.Add("step-begin:" + stepName);
			}

			public void TelemetryStepEnd(string stepName, bool succeeded, double score)
			{
				string scoreText = score.ToString(System.Globalization.CultureInfo.InvariantCulture);
				Calls.Add("step-end:" + stepName + ":" + succeeded + ":" + scoreText);
			}

			public bool TelemetryPoseDue()
			{
				return PoseDue;
			}

			public void TelemetryPose(string trackedId, Vector3 position, Vector3 rotation)
			{
				Calls.Add("pose:" + trackedId);
			}

			public void TelemetryFlush()
			{
				Calls.Add("flush");
			}
		}

		private sealed class RecordingProvider : IApexAnalyticsProvider
		{
			public RecordingProvider(string providerName)
			{
				Name = providerName;
			}

			public string Name { get; }
			public List<string> Calls { get; } = new List<string>();
			public ApexTrackedObject LastTrackedObject { get; private set; }

			public void OnTrackedObjectRegistered(ApexTrackedObject trackedObject)
			{
				LastTrackedObject = trackedObject;
			}

			public void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement)
			{
				LastTrackedObject = trackedObject;
				Calls.Add("begin:" + engagement);
			}

			public void OnEngagementEnd(ApexTrackedObject trackedObject, string engagement)
			{
				LastTrackedObject = trackedObject;
				Calls.Add("end:" + engagement);
			}
		}

		private sealed class FakeGazeSource : ApexGazeSource
		{
			private bool m_hasGaze;
			private Ray m_gazeRay;
			private string m_engagement;

			public void SetGaze(bool hasGaze, Ray gazeRay, string engagement)
			{
				m_hasGaze = hasGaze;
				m_gazeRay = gazeRay;
				m_engagement = engagement;
			}

			public override bool TryGetGaze(out Ray ray, out string engagement)
			{
				ray = m_gazeRay;
				engagement = m_engagement;
				return m_hasGaze;
			}
		}
	}
}
