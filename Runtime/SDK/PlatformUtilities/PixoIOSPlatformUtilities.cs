using System;
using System.Collections.Generic;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
	internal class PixoIOSPlatformUtilities : PixoGenericPlatformUtilities
	{
		public override bool OpenURL(string targetUrl)
		{
			UDebug.Log($"[{GetType().Name}] Opening url {targetUrl}");
			if (string.IsNullOrEmpty(targetUrl))
			{
				UDebug.Log("Url is empty or null.");
				return false;
			}

			Application.OpenURL(targetUrl);
			return true;
		}

		public override bool OpenApplication(string applicationPath, string[] argumentNames, string[] argumentValues)
		{
			return DesktopLaunching.StartApplication(applicationPath, argumentNames, argumentValues);
		}

		public override Dictionary<string, string> ParseApplicationArguments()
		{
#if UNITY_EDITOR
			// The editor has its own command line.
			return new Dictionary<string, string>();
#else
			// A deep link from the launcher carries the arguments in its URL.
			if (!string.IsNullOrEmpty(Application.absoluteURL))
			{
				return ParseURLArguments(Application.absoluteURL);
			}

			return DesktopLaunching.ParseCommandLine(Environment.GetCommandLineArgs());
#endif
		}
	}
}
