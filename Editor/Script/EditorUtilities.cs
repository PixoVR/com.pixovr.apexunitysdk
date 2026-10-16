using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PixoVR.Editor
{
	internal static class EditorUtilities
	{
		public const string kEditorRootPath = "PixoVR/";

		public static TSettings GetProjectSettings<TSettings>(bool includeInPreloadedAssets = true)
			where TSettings : ScriptableObject
		{
			TSettings settings;
#if UNITY_EDITOR
			string defaultPath = $"Assets/Resources/{typeof(TSettings).Name}.asset";
			settings = AssetDatabase.LoadAssetAtPath<TSettings>(defaultPath);
			if (settings == null)
			{
				settings = PlayerSettings.GetPreloadedAssets().OfType<TSettings>().FirstOrDefault();
			}

			if (settings == null)
			{
				TSettings[] allSettings = AssetDatabase.FindAssets($"t:{typeof(TSettings).Name}")
					.Select(AssetDatabase.GUIDToAssetPath)
					.Select(AssetDatabase.LoadAssetAtPath<TSettings>)
					.ToArray();
				if (allSettings.Length == 0)
				{
					if (!AssetDatabase.IsValidFolder("Assets/Resources"))
					{
						AssetDatabase.CreateFolder("Assets", "Resources");
					}

					TSettings createdSettings = ScriptableObject.CreateInstance<TSettings>();
					AssetDatabase.CreateAsset(createdSettings, defaultPath);
					AssetDatabase.SaveAssets();
					settings = createdSettings;
					if (includeInPreloadedAssets)
					{
						Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
						if (!preloadedAssets.Contains(settings))
						{
							PlayerSettings.SetPreloadedAssets(preloadedAssets.Append(settings).ToArray());
						}
					}
				}
				else
				{
					settings = allSettings.First();
				}
			}
#else
			settings = Resources.FindObjectsOfTypeAll<TSettings>().FirstOrDefault();
#endif
			if (settings == null)
			{
				Debug.LogWarning($"Unable to load or create {typeof(TSettings).Name}");
			}

			return settings;
		}

#if UNITY_EDITOR
		/// <summary>The SDK package's root as an asset path, e.g. "Packages/com.pixovr.apexunitysdk".</summary>
		public static string FindPluginPath()
		{
			string[] scriptGuids = AssetDatabase.FindAssets("PixoVRPreBuilder t:Script");
			if (scriptGuids.Length == 0)
			{
				return null;
			}

			string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
			string[] pathParts = scriptPath.Split('/');
			return Path.Combine(pathParts[0], pathParts[1]);
		}
#endif
	}
}
