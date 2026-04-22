using UnityEngine;

namespace PixoVR.Editor
{
    public class PixoVRProjectSettings : ScriptableObject
    {
        [field: SerializeField] public bool SyncVersionsOnBuildPreProcess { get; set; }
        [field: SerializeField] public string CustomURLScheme { get; set; }
        [field: SerializeField] public int BuildPreProcessCallbackOrder { get; set; } = 100;

        [field: SerializeField] public string ModuleVersion { get; set; }
        [field: SerializeField] public bool SyncModuleWithApplicationVersion { get; set; }
    }
}