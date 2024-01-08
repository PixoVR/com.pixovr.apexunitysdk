using Newtonsoft.Json;
using System;

namespace PixoVR.Apex
{
    [Serializable]
    public class AuthorizationCode : IPlatformErrorable
    {
        [JsonProperty(PropertyName = "auth_code")]
        public string Code;

        public bool HasErrored()
        {
            return (Code == null);
        }
    }
}
