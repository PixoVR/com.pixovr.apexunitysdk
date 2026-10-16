using System.Collections.Generic;
using System.Text;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
	/// <summary>Opening URLs and apps, quitting, launch arguments and files, for the current platform.</summary>
	public sealed class PixoPlatformUtilities : PixoSingleton<PixoPlatformUtilities>
	{
		private readonly PixoGenericPlatformUtilities m_platformUtilities;

		public PixoPlatformUtilities()
		{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
			m_platformUtilities = new PixoWindowsPlatformUtilities();
#elif UNITY_WEBGL
			m_platformUtilities = new PixoWebPlatformUtilities();
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
			m_platformUtilities = new PixoOSXPlatformUtilities();
#elif UNITY_ANDROID
			m_platformUtilities = new PixoAndroidPlatformUtilities();
#elif UNITY_IOS
			m_platformUtilities = new PixoIOSPlatformUtilities();
#else
			m_platformUtilities = new PixoGenericPlatformUtilities();
#endif
		}

		public static bool OpenURL(string targetUrl)
		{
			UDebug.Log($"PixoPlatformUtilities::OpenURL {targetUrl}");
			return Instance.m_platformUtilities.OpenURL(targetUrl);
		}

		public static bool OpenApplication(string applicationPath, string[] argumentNames, string[] argumentValues)
		{
			UDebug.Log($"PixoPlatformUtilities::OpenApplication {applicationPath}");
			return Instance.m_platformUtilities.OpenApplication(applicationPath, argumentNames, argumentValues);
		}

		public static void CloseCurrentApplication()
		{
			UDebug.Log("PixoPlatformUtilities::CloseCurrentApplication");
			Instance.m_platformUtilities.CloseCurrentApplication();
		}

		/// <summary>Launch arguments by name (pixotoken, optional, returntarget, targettype), or null.</summary>
		public static Dictionary<string, string> ParseApplicationArguments()
		{
			return Instance.m_platformUtilities.ParseApplicationArguments();
		}

		public static bool ReadFileAsString(string fileName, out string fileText)
		{
			return Instance.m_platformUtilities.ReadFileAsString(fileName, out fileText);
		}

		public static bool ReadFile(string fileName, out byte[] fileBytes)
		{
			return Instance.m_platformUtilities.ReadFile(fileName, out fileBytes);
		}

		public static bool WriteFile(string fileName, byte[] fileBytes)
		{
			return Instance.m_platformUtilities.WriteFile(fileName, fileBytes);
		}

		public static bool WriteStringToFile(string fileName, string fileText, Encoding textEncoding = null)
		{
			return Instance.m_platformUtilities.WriteStringToFile(fileName, fileText, textEncoding);
		}

		public static Dictionary<string, string> ParseURLArguments(string launchUrl)
		{
			return Instance.m_platformUtilities.ParseURLArguments(launchUrl);
		}
	}
}
