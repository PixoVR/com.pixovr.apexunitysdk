#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

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