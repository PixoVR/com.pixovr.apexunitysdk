using UnityEditor;

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
            EditorGUIUtility.labelWidth = 220;

            EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.CustomURLScheme)));
            EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.SyncVersionsOnBuildPreProcess)));
            if (Target.SyncVersionsOnBuildPreProcess)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindBackingFieldProperty(nameof(Target.BuildPreProcessCallbackOrder)));

                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}