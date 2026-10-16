using UnityEngine;

namespace PixoVR.Apex
{
	/// <summary>Android helpers over the Unity activity and the SDK's PixoUtils.java.</summary>
	public static partial class PixoAndroidUtils
	{
		private static AndroidJavaObject m_currentActivity;
		private static AndroidJavaObject m_packageManager;
		private static AndroidJavaObject m_applicationContext;
		private static AndroidJavaObject m_applicationInfo;
		private static AndroidJavaObject m_nativeUtils;

		/// <summary>com.unity3d.player.UnityPlayer.currentActivity</summary>
		public static AndroidJavaObject CurrentActivity
		{
			get
			{
				if (m_currentActivity == null)
				{
					using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
					{
						m_currentActivity = unityPlayer.SafeGetStatic<AndroidJavaObject>("currentActivity");
					}
				}

				return m_currentActivity;
			}
		}

		public static AndroidJavaObject PackageManager
		{
			get
			{
				if (m_packageManager == null)
				{
					m_packageManager = CurrentActivity.SafeCall<AndroidJavaObject>("getPackageManager");
				}

				return m_packageManager;
			}
		}

		/// <summary><see cref="CurrentActivity"/>.getApplicationContext()</summary>
		public static AndroidJavaObject ApplicationContext
		{
			get
			{
				if (m_applicationContext == null)
				{
					m_applicationContext = CurrentActivity.SafeCall<AndroidJavaObject>("getApplicationContext");
				}

				return m_applicationContext;
			}
		}

		/// <summary><see cref="ApplicationContext"/>.getApplicationInfo()</summary>
		public static AndroidJavaObject ApplicationInfo
		{
			get
			{
				if (m_applicationInfo == null)
				{
					m_applicationInfo = ApplicationContext.SafeCall<AndroidJavaObject>("getApplicationInfo");
				}

				return m_applicationInfo;
			}
		}

		/// <summary>An instance of the SDK's com.pixovr.pixosdk.PixoUtils Java class.</summary>
		public static AndroidJavaObject NativeUtils
		{
			get
			{
				if (m_nativeUtils == null)
				{
					m_nativeUtils = new AndroidJavaObject("com.pixovr.pixosdk.PixoUtils", ApplicationContext);
				}

				return m_nativeUtils;
			}
		}

		/// <summary>True when the launching intent's extras contain the key.</summary>
		public static bool HasIntentExtra(string extraName)
		{
			AndroidJavaObject launchIntent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
			AndroidJavaObject extrasBundle = launchIntent.SafeCall<AndroidJavaObject>("getExtras");
			if (extrasBundle == null)
			{
				return false;
			}

			return extrasBundle.SafeCall<bool>("containsKey", extraName);
		}

		/// <summary>A boolean from the launching intent's extras, or defaultValue when it is missing.</summary>
		public static bool GetIntentBooleanExtra(string extraName, bool defaultValue)
		{
			AndroidJavaObject launchIntent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
			return launchIntent.SafeCall<bool>("getBooleanExtra", extraName, defaultValue);
		}

		/// <summary>A string from the launching intent's extras, or null.</summary>
		public static string GetIntentStringExtra(string extraName)
		{
			AndroidJavaObject launchIntent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
			return launchIntent.SafeCall<string>("getStringExtra", extraName);
		}

		public static bool IsAppInstalled(string packageName)
		{
			return NativeUtils != null && NativeUtils.SafeCall<bool>("isAppInstalled", packageName);
		}

		public static void Quit()
		{
			NativeUtils.SafeCall("forceQuit");
		}

		public static bool LaunchApp(string packageName, string[] extraNames, string[] extraValues)
		{
			return NativeUtils.SafeCall<bool>("launchApp", packageName, extraNames, extraValues);
		}

		public static bool LaunchUrl(string launchUrl)
		{
			return NativeUtils.SafeCall("openURL", launchUrl);
		}

		public static string GetPackageLocation(string packageName)
		{
			return NativeUtils.SafeCall<string>("getAppFileLocation", packageName);
		}

		public static string WriteFileToSharedLocation(string fileName, string fileContent)
		{
			return NativeUtils.SafeCall<string>("writeFileToSharedStorage", fileName, fileContent);
		}

		public static string ReadFileFromSharedStorage(string fileName)
		{
			return NativeUtils.SafeCall<string>("readFileFromSharedStorage", fileName);
		}

		public static bool DeleteFileFromSharedStorage(string fileName)
		{
			return NativeUtils.SafeCall<bool>("deleteFileFromSharedStorage", fileName);
		}

		public static bool DoesFileExistInSharedLocation(string fileName)
		{
			return NativeUtils.SafeCall<bool>("fileExists", fileName);
		}
	}
}
