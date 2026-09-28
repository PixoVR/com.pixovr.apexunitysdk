using System.Collections.Generic;
using Newtonsoft.Json;

namespace PixoVR.Apex.Analytics.PixoVR
{
    public sealed class TelemetryPacket
    {
        [JsonProperty("v")]
        public int Version = 1;

        [JsonProperty("seq")]
        public int Sequence;

        [JsonProperty("t0")]
        public long StartUnixMs;

        [JsonProperty("s")]
        public int SessionId;

        [JsonProperty("r", NullValueHandling = NullValueHandling.Ignore)]
        public string Registration;

        [JsonProperty("u")]
        public int UserId;

        [JsonProperty("m")]
        public int ModuleId;

        [JsonProperty("sc", NullValueHandling = NullValueHandling.Ignore)]
        public string ScenarioId;

        [JsonProperty("end", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SessionEnded;

        [JsonProperty("ob")]
        public List<TelemetryObject> Objects = new List<TelemetryObject>();

        [JsonProperty("ev")]
        public List<TelemetryRecord> Records = new List<TelemetryRecord>();
    }

    public sealed class TelemetryObject
    {
        [JsonProperty("i")]
        public int Index;

        [JsonProperty("id")]
        public string TrackedId;

        [JsonProperty("n")]
        public string Name;

        [JsonProperty("mesh", NullValueHandling = NullValueHandling.Ignore)]
        public string MeshName;
    }

    public sealed class TelemetryRecord
    {
        [JsonProperty("t")]
        public int TimeMs;

        [JsonProperty("k")]
        public string Kind;

        [JsonProperty("o", NullValueHandling = NullValueHandling.Ignore)]
        public int? Object;

        [JsonProperty("n", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty("ok", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Success;

        [JsonProperty("sc", NullValueHandling = NullValueHandling.Ignore)]
        public float? Score;

        [JsonProperty("p", NullValueHandling = NullValueHandling.Ignore)]
        public int[] Position;

        [JsonProperty("q", NullValueHandling = NullValueHandling.Ignore)]
        public int[] Rotation;
    }
}
