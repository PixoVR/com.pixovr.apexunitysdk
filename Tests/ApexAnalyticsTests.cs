using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PixoVR.Apex.Analytics;
using TinCan;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixoVR.Apex.Tests
{
    public class ApexAnalyticsTests
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
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }

            ApexAnalytics.Clear();
        }

        [Test]
        public void RegisterReturnsFalseForDuplicateProvider()
        {
            RecordingProvider provider = new RecordingProvider("provider");

            Assert.That(ApexAnalytics.Register(provider), Is.True);
            LogAssert.Expect(LogType.Warning, new Regex("Provider 'provider' is already registered."));
            Assert.That(ApexAnalytics.Register(provider), Is.False);
            LogAssert.Expect(LogType.Warning, new Regex("Provider 'provider' is already registered."));
            Assert.That(ApexAnalytics.Register(new RecordingProvider("provider")), Is.False);
        }

        [Test]
        public void UnregisterRemovesProvider()
        {
            RecordingProvider provider = new RecordingProvider("provider");
            ApexAnalytics.Register(provider);

            Assert.That(ApexAnalytics.Unregister(provider), Is.True);
            Assert.That(ApexAnalytics.Providers, Is.Empty);
            Assert.That(ApexAnalytics.Unregister(provider), Is.False);
        }

        [Test]
        public void DispatchReachesProvidersInRegistrationOrder()
        {
            RecordingProvider first = new RecordingProvider("first");
            RecordingProvider second = new RecordingProvider("second");
            ApexAnalytics.Register(first);
            ApexAnalytics.Register(second);

            ApexAnalytics.Dispatch(provider => provider.OnEngagementBegin(null, "grab"));

            Assert.That(first.Calls, Is.EqualTo(new[] { "begin:grab" }));
            Assert.That(second.Calls, Is.EqualTo(new[] { "begin:grab" }));
        }

        [Test]
        public void DispatchContinuesAfterProviderThrows()
        {
            RecordingProvider throwing = new RecordingProvider("throwing") { ThrowOnBegin = true };
            RecordingProvider receiving = new RecordingProvider("receiving");
            ApexAnalytics.Register(throwing);
            ApexAnalytics.Register(receiving);
            LogAssert.Expect(LogType.Error, new Regex("Provider 'throwing' threw:"));

            Assert.DoesNotThrow(() =>
                ApexAnalytics.Dispatch(provider => provider.OnEngagementBegin(null, "grab")));
            Assert.That(receiving.Calls, Is.EqualTo(new[] { "begin:grab" }));
        }

        [Test]
        public void DispatchWithNoProvidersIsNoOp()
        {
            Assert.DoesNotThrow(() => ApexAnalytics.Dispatch(provider => provider.Flush()));
        }

        [Test]
        public void TrackedObjectGeneratesIdAndDispatchesEngagement()
        {
            RecordingProvider provider = new RecordingProvider("provider");
            ApexAnalytics.Register(provider);

            GameObject gameObject = new GameObject("Tracked Object");
            gameObjects.Add(gameObject);
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();

            Assert.That(trackedObject.TrackedId, Is.Not.Null.And.Not.Empty);

            trackedObject.BeginEngagement("grab");

            Assert.That(provider.LastTrackedObject, Is.SameAs(trackedObject));
            Assert.That(provider.Calls, Does.Contain("registered"));
            Assert.That(provider.Calls, Does.Contain("begin:grab"));
        }

        [Test]
        public void LateRegisteredProviderReceivesExistingTrackedObjects()
        {
            GameObject gameObject = new GameObject("Tracked Object");
            gameObjects.Add(gameObject);
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();
            RecordingProvider provider = new RecordingProvider("provider");

            ApexAnalytics.Register(provider);

            Assert.That(provider.Calls, Does.Contain("registered"));
            Assert.That(provider.LastTrackedObject, Is.SameAs(trackedObject));
        }

        [Test]
        public void DisabledTrackedObjectIsNotReplayed()
        {
            GameObject gameObject = new GameObject("Disabled Tracked Object");
            gameObjects.Add(gameObject);
            gameObject.SetActive(false);
            gameObject.AddComponent<ApexTrackedObject>();
            RecordingProvider provider = new RecordingProvider("provider");

            ApexAnalytics.Register(provider);

            Assert.That(provider.Calls, Does.Not.Contain("registered"));
        }

        [Test]
        public void DuplicateTrackedIdIsRegeneratedOnEnable()
        {
            GameObject firstGameObject = new GameObject("First Tracked Object");
            gameObjects.Add(firstGameObject);
            ApexTrackedObject firstTrackedObject = firstGameObject.AddComponent<ApexTrackedObject>();

            GameObject secondGameObject = new GameObject("Second Tracked Object");
            gameObjects.Add(secondGameObject);
            secondGameObject.SetActive(false);
            ApexTrackedObject secondTrackedObject = secondGameObject.AddComponent<ApexTrackedObject>();
            secondTrackedObject.SetTrackedId(firstTrackedObject.TrackedId);
            secondGameObject.SetActive(true);

            Assert.That(secondTrackedObject.TrackedId, Is.Not.Null.And.Not.Empty);
            Assert.That(secondTrackedObject.TrackedId, Is.Not.EqualTo(firstTrackedObject.TrackedId));
        }

        [Test]
        public void SetTrackedIdRejectsEmpty()
        {
            GameObject gameObject = new GameObject("Tracked Object");
            gameObjects.Add(gameObject);
            ApexTrackedObject trackedObject = gameObject.AddComponent<ApexTrackedObject>();

            Assert.Throws<ArgumentException>(() => trackedObject.SetTrackedId(string.Empty));
            Assert.Throws<ArgumentException>(() => trackedObject.SetTrackedId(null));
        }

        [Test]
        public void SetTrackedIdRegeneratesOnCollisionWithActiveObject()
        {
            GameObject firstGameObject = new GameObject("First Tracked Object");
            gameObjects.Add(firstGameObject);
            ApexTrackedObject firstTrackedObject = firstGameObject.AddComponent<ApexTrackedObject>();

            GameObject secondGameObject = new GameObject("Second Tracked Object");
            gameObjects.Add(secondGameObject);
            ApexTrackedObject secondTrackedObject = secondGameObject.AddComponent<ApexTrackedObject>();

            secondTrackedObject.SetTrackedId(firstTrackedObject.TrackedId);

            Assert.That(secondTrackedObject.TrackedId, Is.Not.Null.And.Not.Empty);
            Assert.That(secondTrackedObject.TrackedId, Is.Not.EqualTo(firstTrackedObject.TrackedId));
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
            public bool ThrowOnBegin { get; set; }

            public void OnSessionEvent(ApexAnalyticsContext context, Statement statement)
            {
                Calls.Add("event");
            }

            public void OnTrackedObjectRegistered(ApexTrackedObject trackedObject)
            {
                LastTrackedObject = trackedObject;
                Calls.Add("registered");
            }

            public void OnTrackedObjectUnregistered(ApexTrackedObject trackedObject)
            {
                LastTrackedObject = trackedObject;
                Calls.Add("unregistered");
            }

            public void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement)
            {
                if (ThrowOnBegin)
                {
                    throw new InvalidOperationException("test provider failure");
                }

                LastTrackedObject = trackedObject;
                Calls.Add($"begin:{engagement}");
            }
        }
    }
}
