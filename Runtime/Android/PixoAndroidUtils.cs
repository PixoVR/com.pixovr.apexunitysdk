using System;

using UnityEngine;

namespace PixoVR.Apex
{
    public static partial class PixoAndroidUtils
    {
        #region JAVA OBJECTS
        /// <summary>
        /// JNI to call com.unity.player.UnityPlayer.currentActivity()
        /// </summary>
        public static AndroidJavaObject CurrentActivity
        {
            get
            {
                if (currentActivity == null)
                {
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    currentActivity = unityPlayer.SafeGetStatic<AndroidJavaObject>("currentActivity");
                }
                return currentActivity;
            }
        }
        static AndroidJavaObject currentActivity;

        public static AndroidJavaObject PackageManager
        {
            get
            {
                if (packageManager == null)
                    packageManager = CurrentActivity.SafeCall<AndroidJavaObject>("getPackageManager");
                return packageManager;
            }
        }
        static AndroidJavaObject packageManager;

        /// <summary>
        /// JNI to call <see cref="CurrentActivity"/>.getApplicationContext()
        /// </summary>
        public static AndroidJavaObject ApplicationContext
        {
            get
            {
                if (applicationContext == null)
                    applicationContext = CurrentActivity.SafeCall<AndroidJavaObject>("getApplicationContext");
                return applicationContext;
            }
        }
        static AndroidJavaObject applicationContext;

        /// <summary>
        /// JNI to call <see cref="ApplicationContext"/>.getApplicationInfo()
        /// </summary>
        public static AndroidJavaObject ApplicationInfo
        {
            get
            {
                if (applicationInfo == null)
                    applicationInfo = ApplicationContext.SafeCall<AndroidJavaObject>("getApplicationInfo");
                return applicationInfo;
            }
        }
        static AndroidJavaObject applicationInfo;

        /// <summary>
        /// Returns an instance of the PixoUtils.java class in the Apex SDK
        /// </summary>
        public static AndroidJavaObject NativeUtils
        {
            get
            {
                if (nativeUtils == null)
                    nativeUtils = new AndroidJavaObject("com.pixovr.pixosdk.PixoUtils", ApplicationContext);
                return nativeUtils;
            }
        }
        static AndroidJavaObject nativeUtils;
        #endregion

        /// <summary>
        /// Returns if the Android intent extras bundle has a key with the given name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasIntentExtra(string key)
        {
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            var bundle = intent.SafeCall<AndroidJavaObject>("getExtras");
            if (bundle == null) return false;
            return bundle.SafeCall<bool>("containsKey", key);
        }

        /// <summary>
        /// Returns a boolean from the Android intent extras 
        /// </summary>
        /// <param name="key">The key to read the boolean from</param>
        /// <param name="defaultValue">The default value in case the key doesn't exist</param>
        /// <returns></returns>
        public static bool GetIntentBooleanExtra(string key, bool defaultValue)
        {
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            return intent.SafeCall<bool>("getBooleanExtra", key, defaultValue);
        }

        /// <summary>
        /// Returns a string from the Android intent extras
        /// </summary>
        /// <param name="key">The key to read the string from</param>
        /// <returns></returns>
        public static string GetIntentStringExtra(string key)
        {
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            return intent.SafeCall<string>("getStringExtra", key);
        }

        public static bool IsAppInstalled(string packageName)
        {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<bool>("isAppInstalled", packageName);
            return false;
        }

        public static bool LaunchApp(string packageName, string[] extraKeys, string[] extraValues)
        {
            return NativeUtils.SafeCall<bool>("launchApp", packageName, extraKeys, extraValues);
        }

        public static bool LaunchUrl(string url)
        {
            return NativeUtils.SafeCall("openURL", url);
        }
    }
}
