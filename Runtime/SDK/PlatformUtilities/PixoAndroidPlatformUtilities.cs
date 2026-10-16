using System.Collections.Generic;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
	internal class PixoAndroidPlatformUtilities : PixoGenericPlatformUtilities
	{
		private static readonly string[] kLaunchExtraNames = { "optional", "returntarget", "targettype", "pixotoken" };

		public override bool OpenURL(string targetUrl)
		{
			return PixoAndroidUtils.LaunchUrl(targetUrl);
		}

		public override bool OpenApplication(string applicationPath, string[] argumentNames, string[] argumentValues)
		{
			return PixoAndroidUtils.LaunchApp(applicationPath, argumentNames, argumentValues);
		}

		public override void CloseCurrentApplication()
		{
			PixoAndroidUtils.CurrentActivity.Call("finishAndRemoveTask");
		}

		/// <summary>The launcher passes arguments as the intent's data URL or as string extras.</summary>
		public override Dictionary<string, string> ParseApplicationArguments()
		{
			using (AndroidJavaObject launchIntent =
				PixoAndroidUtils.CurrentActivity.Call<AndroidJavaObject>("getIntent"))
			{
				string intentData = launchIntent.Call<string>("getDataString");
				if (!string.IsNullOrEmpty(intentData))
				{
					UDebug.Log("[PixoAndroidPlatformUtilities] Parsing launch arguments from the intent URL.");
					return ParseURLArguments(intentData);
				}

				UDebug.Log("[PixoAndroidPlatformUtilities] Parsing launch arguments from intent extras.");
				Dictionary<string, string> launchArguments = new Dictionary<string, string>();
				foreach (string extraName in kLaunchExtraNames)
				{
					string extraValue = launchIntent.Call<string>("getStringExtra", extraName);
					if (!string.IsNullOrEmpty(extraValue))
					{
						launchArguments[extraName] = extraValue;
					}
				}

				return launchArguments;
			}
		}
	}
}
