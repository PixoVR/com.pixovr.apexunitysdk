
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

namespace PixoVR.Apex
{
    internal sealed class PixoPlatformUtilities : PixoSingleton<PixoPlatformUtilities>
    {
        private PixoGenericPlatformUtilities PlatformUtilities;
        public PixoPlatformUtilities()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
            PlatformUtilities = new PixoWindowsPlatformUtilities();
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            PlatformUtilities = new PixoOSXPlatformUtilities();
#elif UNITY_ANDROID
            PlatformUtilities = new PixoAndroidPlatformUtilities();
#else
            PlatformUtilities = new PixoGenericPlatformUtilities();
#endif
        }

        public static bool OpenURL(string url)
        {
            return Instance._OpenURL(url);
        }

        public bool _OpenURL(string url)
        {
            return PlatformUtilities.OpenURL(url);
        }

        public static bool OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            return Instance._OpenApplication(applicationPath, argumentKeys, argumentValues);
        }

        public bool _OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            return PlatformUtilities.OpenApplication(applicationPath, argumentKeys, argumentValues);
        }

        public static Dictionary<string, string> ParseApplicationArguments()
        {
            return Instance._ParseApplicationArguments();
        }

        public Dictionary<string, string> _ParseApplicationArguments()
        {
            return PlatformUtilities.ParseApplicationArguments();
        }
    }
}
