using System;
using System.Collections.Generic;
using PixoVR.Apex;
using PixoVR.Apex.XAPI;
using TinCan;
using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
    public sealed class PixoVRProviderOptions
    {
        public float FlushIntervalSeconds = 10f;
        public float PoseSampleHz = 2f;
        public int MaxRecordsPerPacket = 500;
    }

    public sealed class PixoVRAnalyticsProvider : IApexAnalyticsProvider
    {
        private sealed class PoseSample
        {
            public int[] Position;
            public int[] Rotation;

            public bool Equals(int[] position, int[] rotation)
            {
                return Equal(Position, position) && Equal(Rotation, rotation);
            }

            private static bool Equal(int[] left, int[] right)
            {
                if (left == null || right == null)
                    return left == right;

                return left[0] == right[0] &&
                       left[1] == right[1] &&
                       left[2] == right[2];
            }
        }

        private readonly IApexTelemetrySink sink;
        private readonly PixoVRProviderOptions options;
        private readonly Func<double> clock;
        private readonly Func<long> unixMs;
        private readonly Dictionary<string, int> objectIndexes = new Dictionary<string, int>();
        private readonly Dictionary<ApexTrackedObject, PoseSample> objectPoses =
            new Dictionary<ApexTrackedObject, PoseSample>();
        private readonly List<TelemetryObject> objects = new List<TelemetryObject>();
        private readonly List<TelemetryRecord> records = new List<TelemetryRecord>();

        private double packetStartClock;
        private long packetStartUnixMs;
        private double lastFlush;
        private double lastPoseSample = double.NegativeInfinity;
        private int sequence;
        private int sessionId;
        private int userId;
        private int moduleId;
        private string registration;
        private string scenarioId;

        public PixoVRAnalyticsProvider(
            IApexTelemetrySink sink,
            PixoVRProviderOptions options,
            Func<double> clock = null,
            Func<long> unixMs = null)
        {
            this.sink = sink ?? new LogTelemetrySink();
            this.options = options ?? new PixoVRProviderOptions();
            this.clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
            this.unixMs = unixMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            lastFlush = this.clock();
        }

        public string Name => "PixoVR";

        public void OnUserIdentified(ApexAnalyticsContext context, LoginResponseContent user)
        {
            userId = context != null ? context.UserId : user?.ID ?? 0;
        }

        public void OnSessionJoined(ApexAnalyticsContext context, JoinSessionResponse session)
        {
            CaptureContext(context);
            lastFlush = clock();
            PixoVRTelemetryRunner.Start(this);
        }

        public void OnSessionEvent(ApexAnalyticsContext context, Statement statement)
        {
        }

        public void OnSessionCompleted(ApexAnalyticsContext context, SessionData data)
        {
            Flush(true);
            sessionId = 0;
            registration = null;
            userId = 0;
            moduleId = 0;
            scenarioId = null;
        }

        public void OnTrackedObjectRegistered(ApexTrackedObject trackedObject)
        {
            if (trackedObject == null)
                return;

            double now = clock();
            TelemetryRecord record = NewRecord("r", now);
            record.Object = GetObjectIndex(trackedObject);
            SetPose(record, trackedObject.transform.position, trackedObject.transform.eulerAngles);
            objectPoses[trackedObject] = new PoseSample
            {
                Position = record.Position,
                Rotation = record.Rotation
            };
            AddRecord(record, now);
        }

        public void OnTrackedObjectUnregistered(ApexTrackedObject trackedObject)
        {
            if (trackedObject == null)
                return;

            double now = clock();
            TelemetryRecord record = NewRecord("u", now);
            record.Object = GetObjectIndex(trackedObject);
            SetPose(record, trackedObject.transform.position, trackedObject.transform.eulerAngles);
            AddRecord(record, now);
            objectPoses.Remove(trackedObject);
        }

        public void OnEngagementBegin(ApexTrackedObject trackedObject, string engagement)
        {
            AddObjectRecord("e", trackedObject, engagement);
        }

        public void OnEngagementEnd(ApexTrackedObject trackedObject, string engagement)
        {
            AddObjectRecord("x", trackedObject, engagement);
        }

        public void OnInteraction(ApexTrackedObject trackedObject, string action)
        {
            AddObjectRecord("i", trackedObject, action);
        }

        public void OnStepBegin(ApexAnalyticsContext context, string step)
        {
            AddNamedRecord("sb", step);
        }

        public void OnStepEnd(ApexAnalyticsContext context, string step, bool success, float score)
        {
            double now = clock();
            TelemetryRecord record = NewRecord("se", now);
            record.Name = step;
            record.Success = success;
            record.Score = score;
            AddRecord(record, now);
        }

        public void Flush()
        {
            Flush(false);
        }

        public void Tick(double now)
        {
            double poseInterval = options.PoseSampleHz > 0f
                ? 1d / options.PoseSampleHz
                : 0d;
            if (now - lastPoseSample >= poseInterval)
            {
                SampleHead(now);
                foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
                {
                    if (trackedObject != null && trackedObject.TrackPose)
                    {
                        SampleObject(trackedObject, now);
                    }
                }

                lastPoseSample = now;
            }

            if (records.Count > 0 &&
                now - lastFlush >= options.FlushIntervalSeconds)
            {
                Flush(false, now);
            }
        }

        private void CaptureContext(ApexAnalyticsContext context)
        {
            if (context == null)
                return;

            sessionId = context.SessionId;
            registration = context.SessionRegistration == Guid.Empty
                ? null
                : context.SessionRegistration.ToString("N");
            userId = context.UserId;
            moduleId = context.ModuleId;
            scenarioId = context.ScenarioId;
        }

        private void AddObjectRecord(string kind, ApexTrackedObject trackedObject, string name)
        {
            if (trackedObject == null)
                return;

            double now = clock();
            TelemetryRecord record = NewRecord(kind, now);
            record.Object = GetObjectIndex(trackedObject);
            record.Name = name;
            AddRecord(record, now);
        }

        private void AddNamedRecord(string kind, string name)
        {
            double now = clock();
            TelemetryRecord record = NewRecord(kind, now);
            record.Name = name;
            AddRecord(record, now);
        }

        private TelemetryRecord NewRecord(string kind, double now)
        {
            EnsurePacketStart(now);
            return new TelemetryRecord
            {
                TimeMs = Mathf.Max(0, Mathf.RoundToInt((float)((now - packetStartClock) * 1000d))),
                Kind = kind
            };
        }

        private void AddRecord(TelemetryRecord record, double now)
        {
            records.Add(record);
            if (options.MaxRecordsPerPacket > 0 &&
                records.Count >= options.MaxRecordsPerPacket)
            {
                Flush(false, now);
            }
        }

        private void EnsurePacketStart(double now)
        {
            if (records.Count == 0)
            {
                packetStartClock = now;
                packetStartUnixMs = unixMs();
            }
        }

        private int GetObjectIndex(ApexTrackedObject trackedObject)
        {
            string trackedId = trackedObject.TrackedId;
            int index;
            if (objectIndexes.TryGetValue(trackedId, out index))
                return index;

            index = objects.Count;
            objectIndexes.Add(trackedId, index);
            objects.Add(new TelemetryObject
            {
                Index = index,
                TrackedId = trackedId,
                Name = trackedObject.DisplayName,
                MeshName = trackedObject.MeshName
            });
            return index;
        }

        private void SampleHead(double now)
        {
            Transform head = ApexSpatialSampler.Head;
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            if (head == null)
                return;

            int[] position = QuantizePosition(head.position);
            int[] rotation = QuantizeRotation(head.eulerAngles);
            if (!headPose.Equals(position, rotation))
            {
                TelemetryRecord record = NewRecord("p", now);
                record.Position = position;
                record.Rotation = rotation;
                AddRecord(record, now);
                headPose = new PoseSample { Position = position, Rotation = rotation };
            }
        }

        private void SampleObject(ApexTrackedObject trackedObject, double now)
        {
            int[] position = QuantizePosition(trackedObject.transform.position);
            int[] rotation = QuantizeRotation(trackedObject.transform.eulerAngles);
            PoseSample previous;
            if (objectPoses.TryGetValue(trackedObject, out previous) &&
                previous.Equals(position, rotation))
            {
                return;
            }

            TelemetryRecord record = NewRecord("p", now);
            record.Object = GetObjectIndex(trackedObject);
            record.Position = position;
            record.Rotation = rotation;
            AddRecord(record, now);
            objectPoses[trackedObject] = new PoseSample { Position = position, Rotation = rotation };
        }

        private void Flush(bool sessionEnded, double now)
        {
            if (records.Count == 0 && !sessionEnded)
                return;

            TelemetryPacket packet = new TelemetryPacket
            {
                Sequence = ++sequence,
                StartUnixMs = records.Count == 0 ? unixMs() : packetStartUnixMs,
                SessionId = sessionId,
                Registration = registration,
                UserId = userId,
                ModuleId = moduleId,
                ScenarioId = scenarioId,
                SessionEnded = sessionEnded ? true : (bool?)null,
                Objects = new List<TelemetryObject>(objects),
                Records = new List<TelemetryRecord>(records)
            };

            records.Clear();
            objects.Clear();
            objectIndexes.Clear();
            packetStartClock = 0d;
            packetStartUnixMs = 0L;
            lastFlush = now;

            try
            {
                sink.Send(packet, success =>
                {
                    if (!success)
                    {
                        Debug.LogWarning("[PixoVRAnalytics] Telemetry sink failed.");
                    }
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[PixoVRAnalytics] Telemetry sink threw: " + exception.Message);
            }
        }

        private void Flush(bool sessionEnded)
        {
            Flush(sessionEnded, clock());
        }

        private PoseSample headPose = new PoseSample();

        private static void SetPose(TelemetryRecord record, Vector3 position, Vector3 rotation)
        {
            record.Position = QuantizePosition(position);
            record.Rotation = QuantizeRotation(rotation);
        }

        private static int[] QuantizePosition(Vector3 position)
        {
            return new[]
            {
                Mathf.RoundToInt(position.x * 100f),
                Mathf.RoundToInt(position.y * 100f),
                Mathf.RoundToInt(position.z * 100f)
            };
        }

        private static int[] QuantizeRotation(Vector3 rotation)
        {
            return new[]
            {
                Mathf.RoundToInt(rotation.x),
                Mathf.RoundToInt(rotation.y),
                Mathf.RoundToInt(rotation.z)
            };
        }
    }
}
