using Newtonsoft.Json.Linq;
using TinCan;
using TinCan.Json;

namespace PixoVR.Apex.XAPI
{
    public class JoinSessionData : JsonModel
    {
        public string Uuid;
        public string EventType;
        public int ModuleId;
        public string DeviceId;
        public string IpAddress;
        public Statement JsonData;

        public JoinSessionData()
        {
            JsonData = new Statement();
        }

        public JoinSessionData(Statement sessionData)
        {
            JsonData = sessionData;
        }

        public JoinSessionData(StringOfJSON json) : this(json.toJObject()) { }

        public JoinSessionData(JObject jobj)
        {
            if (jobj["uuid"] != null)
            {
                Uuid = jobj.Value<string>("uuid");
            }
            if (jobj["eventType"] != null)
            {
                EventType = jobj.Value<string>("eventType");
            }
            if (jobj["moduleId"] != null)
            {
                ModuleId = jobj.Value<int>("moduleId");
            }
            if (jobj["deviceId"] != null)
            {
                DeviceId = jobj.Value<string>("deviceId");
            }
            if (jobj["ipAddress"] != null)
            {
                IpAddress = jobj.Value<string>("ipAddress");
            }

            if (jobj["jsonData"] != null)
            {
                JsonData = new Statement(jobj.Value<JObject>("jsonData"));
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            JObject result = new JObject();

            if (Uuid != null)
            {
                result.Add("uuid", Uuid);
            }

            if (EventType != null)
            {
                result.Add("eventType", EventType);
            }

            result.Add("moduleId", ModuleId);

            if (DeviceId != null)
            {
                result.Add("deviceId", DeviceId);
            }

            if (IpAddress != null)
            {
                result.Add("ipAddress", IpAddress);
            }

            // Update the time stamp
            JsonData.Stamp();

            if (JsonData != null)
            {
                result.Add("jsonData", JsonData.ToJObject(version));
            }

            return result;
        }
    }

    public class CompleteSessionData : JsonModel
    {
        public string Uuid;
        public string EventType;
        public int ModuleId;
        public string DeviceId;
        public Statement JsonData;

        // TODO: Remove all these variables below. Waiting on API changed.
        public float Score;
        public float ScoreScaled;
        public float ScoreMin;
        public float ScoreMax;
        public int SessionDuration;

        public CompleteSessionData()
        {
            JsonData = new Statement();
        }

        public CompleteSessionData(Statement sessionData)
        {
            JsonData = sessionData;
        }

        public CompleteSessionData(StringOfJSON json) : this(json.toJObject()) { }

        public CompleteSessionData(JObject jobj)
        {
            if (jobj["uuid"] != null)
            {
                Uuid = jobj.Value<string>("uuid");
            }
            if (jobj["eventType"] != null)
            {
                EventType = jobj.Value<string>("eventType");
            }
            if (jobj["moduleId"] != null)
            {
                ModuleId = jobj.Value<int>("moduleId");
            }
            if (jobj["deviceId"] != null)
            {
                DeviceId = jobj.Value<string>("deviceId");
            }
            if(jobj["score"] != null)
            {
                Score = jobj.Value<float>("score");
            }
            if (jobj["scoreMin"] != null)
            {
                ScoreMin = jobj.Value<float>("scoreMin");
            }
            if (jobj["scoreMax"] != null)
            {
                ScoreMax = jobj.Value<float>("scoreMax");
            }
            if (jobj["scoreScaled"] != null)
            {
                ScoreScaled = jobj.Value<float>("scoreScaled");
            }
            if (jobj["sessionDuration"] != null)
            {
                SessionDuration = jobj.Value<int>("sessionDuration");
            }

            if (jobj["jsonData"] != null)
            {
                JsonData = new Statement(jobj.Value<JObject>("jsonData"));
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            JObject result = new JObject();

            if (Uuid != null)
            {
                result.Add("uuid", Uuid);
            }

            if (EventType != null)
            {
                result.Add("eventType", EventType);
            }

            result.Add("moduleId", ModuleId);

            if (DeviceId != null)
            {
                result.Add("deviceId", DeviceId);
            }

            //result.Add("score", Score);
            //result.Add("scoreMin", ScoreMin);
            //result.Add("scoreMax", ScoreMax);
            //result.Add("scoreScaled", ScoreScaled);
            //result.Add("sessionDuration", SessionDuration);

            // Update the time stamp
            JsonData.Stamp();

            if (JsonData != null)
            {
                JObject jsonDataJObject = JsonData.ToJObject(version);
                jsonDataJObject.Add("score", Score);
                jsonDataJObject.Add("scoreMin", ScoreMin);
                jsonDataJObject.Add("scoreMax", ScoreMax);
                jsonDataJObject.Add("scoreScaled", ScoreScaled);
                jsonDataJObject.Add("sessionDuration", SessionDuration);
                jsonDataJObject.Add("lessonStatus", (JsonData.result != null) ? (JsonData.result.completion == true ? "passed" : "failed") : "failed");
                jsonDataJObject.Add("moduleName", ModuleId.ToString());

                result.Add("jsonData", jsonDataJObject);
            }

            return result;
        }
    }
}
