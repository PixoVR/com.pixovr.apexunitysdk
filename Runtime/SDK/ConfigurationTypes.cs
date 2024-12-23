using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex
{
    [Serializable]
    public class ConfigurationTypes
    {
        public string Platform;
        public string ConfigTarget;

        public ConfigurationTypes(JObject tokenObject)
        {
            Platform = tokenObject.Value<string>("platform");
            ConfigTarget = tokenObject.Value<string>("configTarget");
        }
    }
}
