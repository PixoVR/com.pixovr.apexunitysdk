using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using PixoVR.Apex;
using UnityEditor;
using System.IO;

namespace PixoVR.Editor
{
    public class PixoVRPreBuilder : IPreprocessBuildWithReport
    {
        internal const string CoreName = "Settings";
        internal const string CorePath = "PixoVR/" + CoreName + "/";

        public static PixoVRProjectSettings ProjectSettings =>
            m_projectSettings == null ? m_projectSettings = EditorUtilities.GetProjectSettings<PixoVRProjectSettings>(false) : m_projectSettings;
        private static PixoVRProjectSettings m_projectSettings;

        public int callbackOrder => ProjectSettings.BuildPreProcessCallbackOrder;


        public void OnPreprocessBuild(BuildReport report)
        {
            string pluginRootPath = FindPluginPath();
            if (pluginRootPath == null)
            {
                throw new BuildFailedException("Build aborted: PixoVR Plugin path was not valid.");
            }

            if (report.summary.result is BuildResult.Failed or BuildResult.Cancelled)
                return;

            if(report.summary.platformGroup == BuildTargetGroup.Android)
            {
                string androidManifestPath = Path.Combine(pluginRootPath, "Plugins/Android/AndroidManifest.xml");
                string androidJavaUtilsPath = Path.Combine(pluginRootPath, "Plugins/Android/PixoUtils.java");

                bool foundAndroidFiles = File.Exists(androidManifestPath) && File.Exists(androidJavaUtilsPath);

                if (!foundAndroidFiles)
                {
                    throw new BuildFailedException("Build aborted: Please run the PixoVR Setup. Look in the PixoVR menu in the toolbar.");
                }
            }

            if (!ProjectSettings.SyncVersionsOnBuildPreProcess)
                return;

            Debug.Log("PixoVR Build Preprocess Ran!");
            Debug.Log($"Module version is {ApexSystem.ModuleVersion}.");
            PlayerSettings.bundleVersion = ApexSystem.ModuleVersion;
            Debug.Log($"Player version is {PlayerSettings.bundleVersion}.");
        }

        string FindPluginPath()
        {
            string[] guids = AssetDatabase.FindAssets("PixoVRPreBuilder t:Script");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                string[] pathParts = path.Split("/");
                path = Path.Combine(pathParts[0], pathParts[1]);
                Debug.Log("Prebuild Script path: " + path);
                return path;
            }

            return null;
        }
    }
}
