#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PixoVR.Editor
{
	[CustomEditor(typeof(PixoVRProjectSettings))]
	internal class PixoVRProjectSettingsEditor : ProjectSettingsEditor<PixoVRProjectSettings>
	{
		internal const string kSettingsPath = "Project/PixoVR";

		// "MAJOR.MINOR.PATCH": a major that does not start with 0, one- or two-digit minor and patch.
		// The core's ApexModuleVersionIsValid applies the same rule at runtime.
		private static readonly Regex kModuleVersionPattern = new Regex(@"^[1-9][0-9]*\.[0-9]{1,2}\.[0-9]{1,2}$");

		[SettingsProvider]
		private static SettingsProvider GetSettingsProvider()
		{
			return GetSettingsProvider(PixoVRPreBuilder.ProjectSettings, kSettingsPath,
				typeof(PixoVRProjectSettingsEditor));
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			GUIStyle warningLabelStyle = new GUIStyle(EditorStyles.label);
			warningLabelStyle.normal.textColor = Color.red;
			warningLabelStyle.alignment = TextAnchor.MiddleRight;
			EditorGUIUtility.labelWidth = 220;

			DrawProperty(nameof(Target.PlatformTargetServer), new GUIContent("Target Server"));
			DrawProperty(nameof(Target.ModuleVersion));
			if (!kModuleVersionPattern.IsMatch(Target.ModuleVersion ?? string.Empty))
			{
				EditorGUILayout.LabelField($"Module version {Target.ModuleVersion} is invalid.", warningLabelStyle);
			}

			DrawProperty(nameof(Target.ModuleName));
			DrawProperty(nameof(Target.ModuleID));
			if (Target.ModuleID <= 0)
			{
				EditorGUILayout.LabelField($"Module ID {Target.ModuleID} is invalid. Make sure it is greater than 0.",
					warningLabelStyle);
			}

			DrawProperty(nameof(Target.DefaultScenarioName));
			DrawProperty(nameof(Target.SendXapiSessionEvents), new GUIContent("Send xAPI Session Events",
				"Sessions send xAPI statements, as the Unity SDK 1.x did (the default), until the platform's new "
					+ "session route is live. Off sends the module's data in the new format."));
			DrawProperty(nameof(Target.CustomURLScheme));
			DrawProperty(nameof(Target.SyncVersionsOnBuildPreProcess));
			if (Target.SyncVersionsOnBuildPreProcess)
			{
				EditorGUI.indentLevel++;
				DrawProperty(nameof(Target.BuildPreProcessCallbackOrder));
				EditorGUI.indentLevel--;
			}

			if (serializedObject.ApplyModifiedProperties() && Target.SyncVersionsOnBuildPreProcess)
			{
				PlayerSettings.bundleVersion = Target.ModuleVersion;
			}
		}

		private void DrawProperty(string propertyName, GUIContent propertyLabel = null)
		{
			SerializedProperty backingField = serializedObject.FindBackingFieldProperty(propertyName);
			if (propertyLabel == null)
			{
				EditorGUILayout.PropertyField(backingField);
			}
			else
			{
				EditorGUILayout.PropertyField(backingField, propertyLabel);
			}
		}
	}
}
#endif
