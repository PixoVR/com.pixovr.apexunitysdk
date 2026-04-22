#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public static class PixoVREditorCommands
{
    [MenuItem("PixoVR/Setup")]
    public static void PixoVRPluginSetup()
    {
        string pluginPath = FindPluginPath();
        string destinationManifestPath = Path.Combine(pluginPath, "Plugins/Android/AndroidManifest.xml");
        string destinationJavaUtilsPath = Path.Combine(pluginPath, "Plugins/Android/PixoUtils.java");
        string sourceManifestPath = "";
        string sourceJavaUtilsPath = "";
#if UNITY_6000_0_OR_NEWER
        sourceManifestPath = Path.Combine(pluginPath, "Editor/Others/AndroidManifest_Game.xml");
        sourceJavaUtilsPath = Path.Combine(pluginPath, "Editor/Others/PixoUtils_Game.java");
#else
        sourceManifestPath = Path.Combine(pluginPath, "Editor/Others/AndroidManifest_Main.xml");
        sourceJavaUtilsPath = Path.Combine(pluginPath, "Editor/Others/PixoUtils_Main.java");
#endif
        File.Copy(sourceManifestPath, destinationManifestPath, true);
        File.Copy(sourceJavaUtilsPath, destinationJavaUtilsPath, true);
        AssetDatabase.Refresh();
    }

    [MenuItem("PixoVR/Development/Update Plugin Version")]
    public static async Task PixoVRVersionUpdate()
    {
        await VersionUpdate();

    }

    public static async Task VersionUpdate()
    {
        var packageList = Client.List(true);

        while (!packageList.IsCompleted)
        {
            await Task.Delay(500);
        }

        if (packageList.Status != StatusCode.Success)
        {
            Debug.Log("Failed to get a list of packages.");
            return;
        }
        else
        {
            Debug.Log("Was successful in finding all of the packages.");
        }

        string packageVersion = "", packageAssetPath = "";
        foreach (var package in packageList.Result)
        {
            if (package.name.Equals("com.pixovr.apexunitysdk"))
            {
                Debug.Log($"Package info: {package.name} v{package.version} - {package.assetPath}");
                packageVersion = package.version;
                packageAssetPath = package.assetPath;
            }
        }

        if(string.IsNullOrEmpty(packageVersion))
        {
            Debug.LogError($"Failed to find the PixoVR SDK package or it did not contain a valid version.");
            return;
        }

        if (string.IsNullOrEmpty(packageAssetPath))
        {
            Debug.LogError($"Failed to find the PixoVR SDK package location.");
            return;
        }

        string projectAssetsPath = Application.dataPath;

        // Navigate up one level to get the project root path
        string projectRootPath = Directory.GetParent(projectAssetsPath).FullName;
        string generatedPixoUtils = $"namespace PixoVR.Apex.Utils {{ public static partial class ApexUtils {{ public static string SDKVersion => \"{packageVersion}\"; }} }}";
        string generatedUtilsFile = Path.Combine(projectRootPath, packageAssetPath, "Runtime/SDK/ApexUtilsGenerated.cs");

        Debug.Log($"Generated file {generatedUtilsFile}");

        if(File.Exists(generatedUtilsFile))
        {
            File.Delete(generatedUtilsFile);
        }

        File.WriteAllText(generatedUtilsFile, generatedPixoUtils);

        AssetDatabase.Refresh();

        return;
    }

    private static string FindPluginPath()
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
#endif