#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using PixoVR.Editor;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class PixoVRPostBuilder
{
    public static PixoVRProjectSettings ProjectSettings =>
            m_projectSettings == null ? m_projectSettings = EditorUtilities.GetProjectSettings<PixoVRProjectSettings>(false) : m_projectSettings;
    private static PixoVRProjectSettings m_projectSettings;

    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        PostprocessBuildIOS(target, pathToBuiltProject);
    }

    protected static void PostprocessBuildIOS(BuildTarget target, string pathToBuiltProject)
    {
        if (string.IsNullOrEmpty(ProjectSettings.CustomURLScheme))
            return;

#if UNITY_IOS
        if (target != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        const string urlTypesKey = "CFBundleURLTypes";
        PlistElementArray urlTypes;

        if (rootDict.values.ContainsKey(urlTypesKey))
        {
            urlTypes = rootDict[urlTypesKey].AsArray();
        }
        else
        {
            urlTypes = rootDict.CreateArray(urlTypesKey);
        }

        const string appCategoryKey = "LSApplicationCategoryType";

        if(!rootDict.values.ContainsKey(appCategoryKey))
        {
            rootDict.SetString(appCategoryKey, "public.app-category.educational-games");
        }

        const string useEncryptionKey = "ITSAppUsesNonExemptEncryption";

        if(!rootDict.values.ContainsKey(useEncryptionKey))
        {
            rootDict.SetBoolean(useEncryptionKey, false);
        }

        // Add your custom scheme
        PlistElementDict urlSchemeDict = urlTypes.AddDict();
        PlistElementArray schemesArray = urlSchemeDict.CreateArray("CFBundleURLSchemes");
        schemesArray.AddString(ProjectSettings.CustomURLScheme); // <-- this is your custom scheme (myapp://)

        // Save changes
        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log($"Custom URL scheme added to Info.plist: {ProjectSettings.CustomURLScheme}://");
#endif
    }

}
#endif