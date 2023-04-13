using System;
using UnityEngine;

namespace PixoVR.Apex
{
    public class ApexVerbs
    {
        public static readonly Uri JOINED_SESSION = new Uri("https://pixovr.com/xapi/verbs/joined_session");
        public static readonly Uri SESSION_EVENT = new Uri("https://pixovr.com/xapi/verbs/session_event");
        public static readonly Uri COMPLETED_SESSION = new Uri("https://pixovr.com/xapi/verbs/completed_session");

        protected ApexVerbs() { }
    }

    public class ApexEventTypes
    {
        public const string PIXOVR_SESSION_JOINED = "PIXOVR_SESSION_JOINED";
        public const string PIXOVR_SESSION_EVENT = "PIXOVR_SESSION_EVENT";
        public const string PIXOVR_SESSION_COMPLETE = "PIXOVR_SESSION_COMPLETE";

        protected ApexEventTypes() { }
    }

    public class ApexExtensionStrings
    {
        public static readonly string MODULE_ID = "https://pixovr.com/xapi/extension/moduleIds";

        protected ApexExtensionStrings() { }
    }

    public class ApexEndpoints
    {
        public const string DevelopmentEnvironment = "https://modules.apex.dev.pixovr.com";
        public const string ProductionEnvironment = "https://modules.apex.pixovr.com";
    }
}
