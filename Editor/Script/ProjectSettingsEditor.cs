#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PixoVR.Editor
{
	internal abstract class ProjectSettingsEditor<TAsset> : UnityEditor.Editor where TAsset : ScriptableObject
	{
		protected TAsset Target { get; private set; }

		protected virtual void OnEnable()
		{
			Target = target as TAsset;
		}

		protected virtual void OnDisable()
		{
		}

		public override void OnInspectorGUI()
		{
			DrawInspectorWithoutScriptProperty();
		}

		protected static string GetSettingsPath(string settingsName)
		{
			return $"Project/Resources/{EditorUtilities.kEditorRootPath}{settingsName}";
		}

		protected virtual void DrawInspectorWithoutScriptProperty()
		{
			if (Target == null)
			{
				return;
			}

			serializedObject.Update();
			SerializedProperty visibleProperty = serializedObject.GetIterator();
			if (visibleProperty.NextVisible(true))
			{
				do
				{
					if (visibleProperty.name == "m_Script")
					{
						continue;
					}

					EditorGUILayout.PropertyField(serializedObject.FindProperty(visibleProperty.name), true);
				}
				while (visibleProperty.NextVisible(false));
			}

			serializedObject.ApplyModifiedProperties();
		}

		protected static SettingsProvider GetSettingsProvider(TAsset asset, string settingsPath,
			System.Type customEditorType)
		{
			SettingsProvider settingsProvider = new SettingsProvider(settingsPath, SettingsScope.Project);
			UnityEditor.Editor settingsEditor = CreateEditor(asset, customEditorType);
			settingsProvider.guiHandler += searchContext =>
			{
				settingsEditor.OnInspectorGUI();
			};
			return settingsProvider;
		}
	}
}
#endif
