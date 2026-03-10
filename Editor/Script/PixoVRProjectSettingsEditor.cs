using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PixoVR.Editor
{
    [CustomEditor(typeof(PixoVRProjectSettings))]
    internal class PixoVRProjectSettingsEditor : ProjectSettingsEditor<PixoVRProjectSettings>
    {
        internal static string SettingsPath = $"Project/PixoVR";

        [SettingsProvider]
        static SettingsProvider GetSettingsProvider()
        {
            return GetSettingsProvider(PixoVRPreBuilder.ProjectSettings, SettingsPath,
                typeof(PixoVRProjectSettingsEditor));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIStyle LabelStyle = new GUIStyle(EditorStyles.label);
            LabelStyle.normal.textColor = Color.red;
            LabelStyle.alignment = TextAnchor.MiddleRight;

            EditorGUIUtility.labelWidth = 220;

            EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.ModuleVersion)));
            if(!IsModuleVersionValid(Target.ModuleVersion))
            {
                EditorGUILayout.LabelField($"Module version {Target.ModuleVersion} is invalid.", LabelStyle);
            }

            EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.CustomURLScheme)));
            EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.SyncVersionsOnBuildPreProcess)));
            if (Target.SyncVersionsOnBuildPreProcess)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.BuildPreProcessCallbackOrder)));

                EditorGUI.indentLevel--;
            }

            if(serializedObject.ApplyModifiedProperties())
            {
                if(Target.SyncVersionsOnBuildPreProcess)
                {
                    PlayerSettings.bundleVersion = Target.ModuleVersion;
                }
            }
        }

        bool IsModuleVersionValid(string moduleVersion)
        {
            if (IsModuleVersionOnlyNumerical(moduleVersion) == false)
                return false;

            string[] moduleVersionParts = moduleVersion.Split('.');

            if (moduleVersionParts.Length != 3)
                return false;

            if (!IsModuleMajorVersionPartValid(moduleVersionParts[0]))
                return false;

            if (!IsModuleNonMajorVersionPartValid(moduleVersionParts[1]))
                return false;

            if (!IsModuleNonMajorVersionPartValid(moduleVersionParts[2]))
                return false;

            return true;
        }

        static readonly Regex VersionValidator = new Regex(@"^[0123456789.]+$");

        bool IsModuleVersionOnlyNumerical(string moduleVersion)
        {
            return VersionValidator.IsMatch(moduleVersion);
        }

        bool IsModuleNonMajorVersionPartValid(string modulePart)
        {
            if (modulePart.Length <= 0)
                return false;

            if (modulePart.Length > 2)
                return false;

            return true;
        }

        bool IsModuleMajorVersionPartValid(string modulePart)
        {
            if (modulePart.Length <= 0)
                return false;

            return true;
        }
    }
}