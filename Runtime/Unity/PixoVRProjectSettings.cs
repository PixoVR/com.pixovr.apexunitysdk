using UnityEngine;

namespace PixoVR.Editor
{
	public class PixoVRProjectSettings : ScriptableObject
	{
		[field: SerializeField] public bool SyncVersionsOnBuildPreProcess { get; set; }
		[field: SerializeField] public string CustomURLScheme { get; set; }
		[field: SerializeField] public int BuildPreProcessCallbackOrder { get; set; } = 100;

		[field: SerializeField, PixoVR.Apex.EndpointDisplay]
		public PixoVR.Apex.EPlatformServer PlatformTargetServer { get; set; }

		[field: SerializeField] public string ModuleVersion { get; set; }
		[field: SerializeField] public int ModuleID { get; set; }
		[field: SerializeField] public string ModuleName { get; set; }
		[field: SerializeField] public string DefaultScenarioName { get; set; } = "Default";
		[field: SerializeField] public bool SyncModuleWithApplicationVersion { get; set; }

		/// <summary>
		/// Session calls send xAPI statements, as the Unity SDK 1.x did, until the platform's new session
		/// route is live. On by default, also for settings saved before it existed.
		/// See <see cref="PixoVR.Apex.ApexSystem.SendXapiSessionEvents"/>.
		/// </summary>
		[field: SerializeField] public bool SendXapiSessionEvents { get; set; } = true;
	}
}
