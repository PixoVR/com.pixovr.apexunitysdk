using System;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexAnalyticsContext
    {
        public int UserId;
        public int OrgId;
        public string UserEmail;
        public int ModuleId;
        public string ModuleName;
        public string ModuleVersion;
        public string ScenarioId;
        public int SessionId;
        public Guid SessionRegistration;
        public string DeviceId;
        public string DeviceModel;
        public string DeviceSerial;
        public string Platform;
        public string SdkVersion;
    }
}
