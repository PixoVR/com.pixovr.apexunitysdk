#if UNITY_EDITOR
using System.IO;
using PixoVR.Apex.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixoVR.Editor
{
	public class PixoVRPreBuilder : IPreprocessBuildWithReport
	{
		private static PixoVRProjectSettings m_projectSettings;

		public static PixoVRProjectSettings ProjectSettings
		{
			get
			{
				if (m_projectSettings == null)
				{
					m_projectSettings = EditorUtilities.GetProjectSettings<PixoVRProjectSettings>(false);
				}

				return m_projectSettings;
			}
		}

		public int callbackOrder => ProjectSettings.BuildPreProcessCallbackOrder; // StyleCheck: external name

		public void OnPreprocessBuild(BuildReport buildReport)
		{
			string pluginRootPath = EditorUtilities.FindPluginPath();
			if (pluginRootPath == null)
			{
				throw new BuildFailedException("Build aborted: PixoVR Plugin path was not valid.");
			}

			if (buildReport.summary.result is BuildResult.Failed or BuildResult.Cancelled)
			{
				return;
			}

			if (buildReport.summary.platformGroup == BuildTargetGroup.Android)
			{
				string androidManifestPath = Path.Combine(pluginRootPath, "Plugins/Android/AndroidManifest.xml");
				string androidJavaUtilsPath = Path.Combine(pluginRootPath, "Plugins/Android/PixoUtils.java");
				bool foundAndroidFiles = File.Exists(androidManifestPath) && File.Exists(androidJavaUtilsPath);
				if (!foundAndroidFiles)
				{
					throw new BuildFailedException(
						"Build aborted: Please run the PixoVR Setup. Look in the PixoVR menu in the toolbar.");
				}
			}

			CheckNativeLibrary(pluginRootPath, buildReport.summary.platform);
			if (ApexSourceHash.IsUpToDate(out string currentSourceHash))
			{
				Debug.Log($"Apex SDK source hash is {ApexUtils.SDKSourceHash}.");
			}
			else
			{
				Debug.LogWarning($"Apex SDK source hash {ApexUtils.SDKSourceHash} does not match the SDK sources "
					+ $"({currentSourceHash}). The SDK was modified without regenerating "
					+ "Runtime/SDK/ApexSourceHashGenerated.cs.");
			}

			if (!ProjectSettings.SyncVersionsOnBuildPreProcess)
			{
				return;
			}

			Debug.Log($"Module version is {ProjectSettings.ModuleVersion}.");
			PlayerSettings.bundleVersion = ProjectSettings.ModuleVersion;
			Debug.Log($"Player version is {PlayerSettings.bundleVersion}.");
		}

		/// <summary>Warns when the build target has no apex_core library: every platform call would fail.</summary>
		private static void CheckNativeLibrary(string pluginRootPath, BuildTarget buildTarget)
		{
			string libraryPath;
			switch (buildTarget)
			{
				case BuildTarget.Android:
					libraryPath = "Plugins/Android/arm64-v8a/libapex_core.so";
					break;
				case BuildTarget.StandaloneWindows64:
					libraryPath = "Plugins/Windows/x86_64/apex_core.dll";
					break;
				default:
					Debug.LogWarning($"The Apex core has no native library for {buildTarget} yet; "
						+ "platform calls will fail in this build.");
					return;
			}

			if (!File.Exists(Path.Combine(pluginRootPath, libraryPath)))
			{
				throw new BuildFailedException($"Build aborted: {libraryPath} is missing from the Apex SDK. "
					+ "Run Tools~/update_core.py in the package.");
			}
		}
	}
}
#endif
