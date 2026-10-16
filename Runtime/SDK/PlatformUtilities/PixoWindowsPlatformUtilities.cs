using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
	internal class PixoWindowsPlatformUtilities : PixoGenericPlatformUtilities
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
			Application.Quit();
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
			string[] commandLineArguments = Environment.GetCommandLineArgs();
			// A launcher URL arrives as the only argument.
			if (commandLineArguments.Length == 2)
			{
				return ParseURLArguments(commandLineArguments[1]);
			}

			return DesktopLaunching.ParseCommandLine(commandLineArguments);
#endif
		}

		public override bool ReadFileAsString(string fileName, out string fileText)
		{
			return PersistentDataFiles.ReadText(fileName, out fileText);
		}

		public override bool ReadFile(string fileName, out byte[] fileBytes)
		{
			return PersistentDataFiles.ReadBytes(fileName, out fileBytes);
		}

		public override bool WriteFile(string fileName, byte[] fileBytes)
		{
			return PersistentDataFiles.WriteBytes(fileName, fileBytes);
		}

		public override bool WriteStringToFile(string fileName, string fileText, Encoding textEncoding = null)
		{
			return PersistentDataFiles.WriteText(fileName, fileText, textEncoding);
		}
	}
}
