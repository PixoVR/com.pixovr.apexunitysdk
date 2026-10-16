#if UNITY_EDITOR
using System.IO;
using PixoVR.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class PixoVRPostBuilder
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

	[PostProcessBuild(100)]
	public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
	{
		PostprocessBuildIOS(buildTarget, pathToBuiltProject);
	}

	/// <summary>Registers the custom URL scheme in the iOS Info.plist so launcher deep links reach the app.</summary>
	protected static void PostprocessBuildIOS(BuildTarget buildTarget, string pathToBuiltProject)
	{
		if (string.IsNullOrEmpty(ProjectSettings.CustomURLScheme))
		{
			return;
		}

#if UNITY_IOS
		if (buildTarget != BuildTarget.iOS)
		{
			return;
		}

		string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
		PlistDocument plist = new PlistDocument();
		plist.ReadFromFile(plistPath);
		PlistElementDict rootDictionary = plist.root;

		const string kUrlTypesKey = "CFBundleURLTypes";
		PlistElementArray urlTypes = rootDictionary.values.ContainsKey(kUrlTypesKey)
			? rootDictionary[kUrlTypesKey].AsArray()
			: rootDictionary.CreateArray(kUrlTypesKey);

		const string kAppCategoryKey = "LSApplicationCategoryType";
		if (!rootDictionary.values.ContainsKey(kAppCategoryKey))
		{
			rootDictionary.SetString(kAppCategoryKey, "public.app-category.educational-games");
		}

		const string kUsesEncryptionKey = "ITSAppUsesNonExemptEncryption";
		if (!rootDictionary.values.ContainsKey(kUsesEncryptionKey))
		{
			rootDictionary.SetBoolean(kUsesEncryptionKey, false);
		}

		PlistElementDict urlSchemeDictionary = urlTypes.AddDict();
		PlistElementArray schemes = urlSchemeDictionary.CreateArray("CFBundleURLSchemes");
		schemes.AddString(ProjectSettings.CustomURLScheme);
		plist.WriteToFile(plistPath);
		UnityEngine.Debug.Log($"Custom URL scheme added to Info.plist: {ProjectSettings.CustomURLScheme}://");
#endif
	}
}
#endif
