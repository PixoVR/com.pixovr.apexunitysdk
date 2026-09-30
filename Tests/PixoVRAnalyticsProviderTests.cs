using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PixoVR.Apex.Analytics;
using PixoVR.Apex.Analytics.PixoVR;
using UnityEngine;

namespace PixoVR.Apex.Tests
{
    public class PixoVRAnalyticsProviderTests
    {
        private readonly List<GameObject> gameObjects = new();

        [SetUp]
        public void SetUp()
        {
            ApexAnalytics.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }

            ApexAnalytics.Clear();
        }

        [Test]
        public void RegisterEmitsObjectAndCentimeterPose()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(sink);
            GameObject gameObject = CreateObject("Tracked");
            gameObject.transform.position = new Vector3(1.234f, 0f, -2.01f);
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();

            provider.OnTrackedObjectRegistered(trackedObject);

            Assert.That(sink.Packets, Has.Count.EqualTo(0));
            provider.Flush();
            TelemetryPacket packet = sink.Last;
            Assert.That(packet.Objects[0].TrackedId, Is.EqualTo(trackedObject.TrackedId));
            Assert.That(packet.Records[0].Kind, Is.EqualTo("r"));
            Assert.That(packet.Records[0].Position, Is.EqualTo(new[] { 123, 0, -201 }));
        }

        [Test]
        public void EngagementInteractionAndStepsAreRecorded()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(sink);
            GameObject gameObject = CreateObject("Tracked");
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();

            provider.OnEngagementBegin(trackedObject, "grab");
            provider.OnEngagementEnd(trackedObject, "grab");
            provider.OnInteraction(trackedObject, "use");
            provider.OnStepBegin(null, "step");
            provider.OnStepEnd(null, "step", true, 0.75f);
            provider.Flush();

            Assert.That(sink.Last.Records.FindAll(record => record.Kind == "e"), Has.Count.EqualTo(1));
            Assert.That(sink.Last.Records.FindAll(record => record.Kind == "x"), Has.Count.EqualTo(1));
            Assert.That(sink.Last.Records.FindAll(record => record.Kind == "i"), Has.Count.EqualTo(1));
            TelemetryRecord stepEnd = sink.Last.Records.Find(record => record.Kind == "se");
            Assert.That(stepEnd.Success, Is.True);
            Assert.That(stepEnd.Score, Is.EqualTo(0.75f));
        }

        [Test]
        public void TickFlushesAfterInterval()
        {
            RecordingSink sink = new();
            double now = 0d;
            PixoVRAnalyticsProvider provider = CreateProvider(
                sink,
                new PixoVRProviderOptions { FlushIntervalSeconds = 1f },
                () => now);

            provider.OnStepBegin(null, "step");
            now = 0.5d;
            provider.Tick(now);
            Assert.That(sink.Packets, Is.Empty);
            now = 1.1d;
            provider.Tick(now);
            Assert.That(sink.Packets, Has.Count.EqualTo(1));
        }

        [Test]
        public void MaximumRecordsFlushesImmediately()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(
                sink,
                new PixoVRProviderOptions { MaxRecordsPerPacket = 2 });

            provider.OnStepBegin(null, "one");
            provider.OnStepBegin(null, "two");

            Assert.That(sink.Packets, Has.Count.EqualTo(1));
            Assert.That(sink.Last.Records, Has.Count.EqualTo(2));
        }

        [Test]
        public void UnchangedPoseIsDeduplicated()
        {
            RecordingSink sink = new();
            double now = 0d;
            PixoVRAnalyticsProvider provider = CreateProvider(sink, new PixoVRProviderOptions { PoseSampleHz = 2f }, () => now);
            GameObject headObject = CreateObject("Head");
            headObject.AddComponent<ApexSpatialSampler>();

            provider.Tick(now);
            now = 0.5d;
            provider.Tick(now);
            provider.Flush();

            Assert.That(sink.Last.Records, Has.Count.EqualTo(1));
            Assert.That(sink.Last.Records[0].Kind, Is.EqualTo("p"));
            Assert.That(sink.Last.Records[0].Object, Is.Null);
        }

        [Test]
        public void SessionCompletionSendsEmptyEndingPacket()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(sink);

            provider.OnSessionCompleted(null, null);

            Assert.That(sink.Packets, Has.Count.EqualTo(1));
            Assert.That(sink.Last.SessionEnded, Is.True);
            Assert.That(sink.Last.Records, Is.Empty);
        }

        [Test]
        public void ObjectIndexesRestartForEachPacket()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(sink);
            GameObject gameObject = CreateObject("Tracked");
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();

            provider.OnInteraction(trackedObject, "first");
            provider.Flush();
            provider.OnInteraction(trackedObject, "second");
            provider.Flush();

            Assert.That(sink.Packets[0].Records[0].Object, Is.EqualTo(0));
            Assert.That(sink.Packets[1].Records[0].Object, Is.EqualTo(0));
            Assert.That(sink.Packets[0].Objects[0].Index, Is.EqualTo(0));
            Assert.That(sink.Packets[1].Objects[0].Index, Is.EqualTo(0));
        }

        [Test]
        public void ApexInteractableIgnoresDoubleGrab()
        {
            RecordingSink sink = new();
            PixoVRAnalyticsProvider provider = CreateProvider(sink);
            ApexAnalytics.Register(provider);
            GameObject gameObject = CreateObject("Interactable");
            ApexInteractable interactable = gameObject.AddComponent<ApexInteractable>();

            interactable.Grab();
            interactable.Grab();
            provider.Flush();

            Assert.That(
                sink.Last.Records.FindAll(record => record.Kind == "e" && record.Name == "grab"),
                Has.Count.EqualTo(1));
        }

        [Test]
        public void GazeSourceBeginsAndEndsConfiguredEngagement()
        {
            RecordingProvider provider = new("gaze");
            ApexAnalytics.Register(provider);

            GameObject sourceObject = CreateObject("Gaze");
            FakeGazeSource source = sourceObject.AddComponent<FakeGazeSource>();
            ApexGazeTracker tracker = sourceObject.AddComponent<ApexGazeTracker>();
            typeof(ApexGazeTracker).GetField(
                "dwellSeconds",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tracker, 0f);

            GameObject targetObject = CreateObject("Target");
            targetObject.transform.position = new Vector3(0f, 0f, 3f);
            targetObject.AddComponent<BoxCollider>();
            ApexTrackedObject target = targetObject.AddComponent<ApexTrackedObject>();
            source.SetGaze(true, new Ray(Vector3.zero, Vector3.forward), "eye_gaze");
            Physics.SyncTransforms();

            tracker.Sample();
            tracker.Sample();
            Assert.That(provider.Calls, Does.Contain("begin:eye_gaze"));

            source.SetGaze(false, default, null);
            tracker.Sample();
            Assert.That(provider.Calls, Does.Contain("end:eye_gaze"));
            Assert.That(provider.LastTrackedObject, Is.SameAs(target));
        }

        private PixoVRAnalyticsProvider CreateProvider(
            RecordingSink sink,
            PixoVRProviderOptions options = null,
            Func<double> clock = null)
        {
            return new PixoVRAnalyticsProvider(
                sink,
                options ?? new PixoVRProviderOptions(),
                clock ?? (() => 0d),
                () => 1000L);
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            gameObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class RecordingSink : IApexTelemetrySink
        {
            public List<TelemetryPacket> Packets { get; } = new();

            public TelemetryPacket Last => Packets[Packets.Count - 1];

            public void Send(TelemetryPacket packet, Action<bool> done)
            {
                Packets.Add(packet);
                done?.Invoke(true);
            }
        }

        private sealed class RecordingProvider : IApexAnalyticsProvider
        {
            public RecordingProvider(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public List<string> Calls { get; } = new();
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
            private bool hasGaze;
            private Ray gazeRay;
            private string engagement;

            public void SetGaze(bool valid, Ray ray, string gazeEngagement)
            {
                hasGaze = valid;
                gazeRay = ray;
                engagement = gazeEngagement;
            }

            public override bool TryGetGaze(out Ray ray, out string gazeEngagement)
            {
                ray = gazeRay;
                gazeEngagement = engagement;
                return hasGaze;
            }
        }
    }
}
