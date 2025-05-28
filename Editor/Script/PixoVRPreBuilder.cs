using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using PixoVR.Apex;
using UnityEditor;

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
            if (!ProjectSettings.SyncVersionsOnBuildPreProcess)
                return;

            if (report.summary.result is BuildResult.Failed or BuildResult.Cancelled)
                return;

            Debug.Log("PixoVR Build Preprocess Ran!");
            Debug.Log($"Module version is {ApexSystem.ModuleVersion}.");
            PlayerSettings.bundleVersion = ApexSystem.ModuleVersion;
            Debug.Log($"Player version is {PlayerSettings.bundleVersion}.");
        }
    }
}
