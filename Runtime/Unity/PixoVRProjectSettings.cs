using UnityEngine;

namespace PixoVR.Editor
{
    public class PixoVRProjectSettings : ScriptableObject
    {
        [field: SerializeField] public bool SyncVersionsOnBuildPreProcess { get; set; }
        [field: SerializeField] public string CustomURLScheme { get; set; }
        [field: SerializeField] public int BuildPreProcessCallbackOrder { get; set; } = 100;

        [field: SerializeField, PixoVR.Apex.EndpointDisplay]
        public PixoVR.Apex.PlatformServer PlatformTargetServer { get; set; }

        [field: SerializeField] public string ModuleVersion { get; set; }
        [field: SerializeField] public int ModuleID { get; set; }
        [field: SerializeField] public string ModuleName { get; set; }
        [field: SerializeField] public string DefaultScenarioName { get; set; } = "Default";
        [field: SerializeField] public bool SyncModuleWithApplicationVersion { get; set; }
    }
}