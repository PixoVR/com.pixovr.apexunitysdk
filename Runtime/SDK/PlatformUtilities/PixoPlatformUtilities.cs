using System.Collections.Generic;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
    public sealed class PixoPlatformUtilities : PixoSingleton<PixoPlatformUtilities>
    {
        private PixoGenericPlatformUtilities PlatformUtilities;
        public PixoPlatformUtilities()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
            PlatformUtilities = new PixoWindowsPlatformUtilities();
#elif UNITY_WEBGL
            PlatformUtilities = new PixoWebPlatformUtilities();
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            PlatformUtilities = new PixoOSXPlatformUtilities();
#elif UNITY_ANDROID
            PlatformUtilities = new PixoAndroidPlatformUtilities();
#elif UNITY_IOS
            PlatformUtilities = new PixoIOSPlatformUtilities();
#else
            PlatformUtilities = new PixoGenericPlatformUtilities();
#endif
        }

        public static bool OpenURL(string url)
        {
            UDebug.Log($"PixoPlatformUtilities::OpenURL {url}");
            return Instance.PlatformUtilities.OpenURL(url);
        }

        public static bool OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            UDebug.Log($"PixoPlatformUtilities::OpenApplication {applicationPath}");
            return Instance.PlatformUtilities.OpenApplication(applicationPath, argumentKeys, argumentValues);
        }

        public static void CloseCurrentApplication()
        {
            UDebug.Log($"PixoPlatformUtilities::CloseCurrentApplication");
            Instance.PlatformUtilities.CloseCurrentApplication();
        }

        public static Dictionary<string, string> ParseApplicationArguments()
        {
            return Instance._ParseApplicationArguments();
        }

        public Dictionary<string, string> _ParseApplicationArguments()
        {
            return PlatformUtilities.ParseApplicationArguments();
        }

        public static bool ReadFileAsString(string fileName, out string data)
        {
            return Instance.PlatformUtilities.ReadFileAsString(fileName, out data);
        }

        public static bool ReadFile(string fileName, out byte[] data)
        {
            return Instance.PlatformUtilities.ReadFile(fileName, out data);
        }

        public static bool WriteFile(string fileName, byte[] data)
        {
            return Instance.PlatformUtilities.WriteFile(fileName, data);
        }

        public static bool WriteStringToFile(string fileName, string data, System.Text.Encoding encoding = null)
        {
            return Instance.PlatformUtilities.WriteStringToFile(fileName, data, encoding);
        }

        public static Dictionary<string, string> ParseURLArguments(string url)
        {
            return Instance.PlatformUtilities.ParseURLArguments(url);
        }
    }
}
