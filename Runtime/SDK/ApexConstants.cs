using System;

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

    public class PlatformEndpoints
    {
        public const string NorthAmerica_ProductionEnvironment = "https://modules.apex.pixovr.com";
        public const string NorthAmerica_StagingEnvironment = "https://modules.apex.stage.pixovr.com";
        public const string NorthAmerica_DevelopmentEnvironment = "https://modules.apex.dev.pixovr.com";
        public const string Saudi_ProductionEnvironment = "https://modules.apexsa.pixovr.com";
        public const string Saudi_StagingEnvironment = "https://modules.apexsa.stage.pixovr.com";
        public const string Saudi_DevelopmentEnvironment = "https://modules.apexsa.dev.pixovr.com";
    }

    public class WebPlatformEndpoints
    {
        public const string NorthAmerica_ProductionEnvironment = "https://api.apex.pixovr.com";
        public const string NorthAmerica_StagingEnvironment = "https://api.apex.stage.pixovr.com";
        public const string NorthAmerica_DevelopmentEnvironment = "https://api.apex.dev.pixovr.com";
        public const string Saudi_ProductionEnvironment = "https://api.apexsa.pixovr.com";
        public const string Saudi_StagingEnvironment = "https://api.apexsa.stage.pixovr.com";
        public const string Saudi_DevelopmentEnvironment = "https://api.apexsa.dev.pixovr.com";
    }

    public class APIPlatformEndpoints
    {
        public const string NorthAmerica_ProductionEnvironment = "https://apex.pixovr.com";
        public const string NorthAmerica_StagingEnvironment = "https://apex.stage.pixovr.com";
        public const string NorthAmerica_DevelopmentEnvironment = "https://apex.dev.pixovr.com";
        public const string Saudi_ProductionEnvironment = "https://saudi.apex.pixovr.com";
        public const string Saudi_StagingEnvironment = "https://saudi.apex.stage.pixovr.com";
        public const string Saudi_DevelopmentEnvironment = "https://saudi.apex.dev.pixovr.com";
    }
}
