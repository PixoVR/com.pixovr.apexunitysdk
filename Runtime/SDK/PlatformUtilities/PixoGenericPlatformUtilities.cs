using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
	/// <summary>Platform behavior with safe defaults; each platform overrides what it supports.</summary>
	internal class PixoGenericPlatformUtilities
	{
		public PixoGenericPlatformUtilities()
		{
			UDebug.Log($"Initializing class {GetType().Name}");
		}

		public virtual bool OpenURL(string targetUrl)
		{
			return false;
		}

		public virtual bool OpenApplication(string applicationPath, string[] argumentNames, string[] argumentValues)
		{
			return false;
		}

		/// <summary>Launch arguments by name (pixotoken, optional, returntarget, targettype), or null.</summary>
		public virtual Dictionary<string, string> ParseApplicationArguments()
		{
			return null;
		}

		public virtual void CloseCurrentApplication()
		{
#if UNITY_EDITOR
			if (EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}
#else
			Application.Quit();
#endif
		}

		/// <summary>
		/// Reads "key=value&amp;..." after the first '?'. The whole text is unescaped first, and the
		/// "optional" value may be JSON containing '&amp;', so it runs to its last '}'.
		/// </summary>
		public virtual Dictionary<string, string> ParseURLArguments(string launchUrl)
		{
			if (string.IsNullOrEmpty(launchUrl))
			{
				return null;
			}

			string unescapedUrl = UnityWebRequest.UnEscapeURL(launchUrl);
			string queryText = unescapedUrl.Substring(unescapedUrl.IndexOf('?') + 1);
			if (queryText.Length == 0)
			{
				return null;
			}

			Dictionary<string, string> launchArguments = new Dictionary<string, string>();
			string[] optionalSplit = queryText.Split(new[] { "optional=" }, 2, StringSplitOptions.None);
			if (optionalSplit.Length > 1)
			{
				string textAfterOptional = optionalSplit[1];
				int jsonEnd = textAfterOptional.LastIndexOf('}');
				int optionalEnd = jsonEnd >= 0 ? jsonEnd + 1 : textAfterOptional.IndexOf('&');
				if (optionalEnd < 0)
				{
					optionalEnd = textAfterOptional.Length;
				}

				launchArguments["optional"] = textAfterOptional.Substring(0, optionalEnd);
				queryText = optionalSplit[0].TrimEnd('&') + textAfterOptional.Substring(optionalEnd);
			}

			foreach (string argumentText in queryText.Split('&'))
			{
				string[] nameAndValue = argumentText.Split(new[] { '=' }, 2);
				if (nameAndValue.Length == 2 && nameAndValue[0].Length > 0)
				{
					launchArguments[nameAndValue[0]] = nameAndValue[1];
				}
			}

			return launchArguments;
		}

		public virtual bool ReadFileAsString(string fileName, out string fileText)
		{
			fileText = null;
			return false;
		}

		public virtual bool ReadFile(string fileName, out byte[] fileBytes)
		{
			fileBytes = null;
			return false;
		}

		public virtual bool WriteFile(string fileName, byte[] fileBytes)
		{
			return false;
		}

		public virtual bool WriteStringToFile(string fileName, string fileText, Encoding textEncoding = null)
		{
			return false;
		}
	}

	/// <summary>Launching another executable and reading "-name value" command-line arguments.</summary>
	internal static class DesktopLaunching
	{
		public static bool StartApplication(string applicationPath, string[] argumentNames, string[] argumentValues)
		{
			if (string.IsNullOrEmpty(applicationPath))
			{
				UDebug.Log("No application path given.");
				return false;
			}

			if (!File.Exists(applicationPath))
			{
				UDebug.Log($"Application does not exist at {applicationPath}");
				return false;
			}

			int argumentCount = Mathf.Min(argumentNames.Length, argumentValues.Length);
			if (argumentNames.Length != argumentValues.Length)
			{
				UDebug.LogWarning("The number of argument names and values differ; the extra ones are not passed.");
			}

			// "-name "value"" pairs, quoted so values with spaces stay whole.
			StringBuilder commandLine = new StringBuilder();
			for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
			{
				if (argumentIndex > 0)
				{
					commandLine.Append(' ');
				}

				commandLine.Append('-').Append(argumentNames[argumentIndex]).Append(" \"")
					.Append(argumentValues[argumentIndex]).Append('"');
			}

			using (Process launchedProcess = new Process())
			{
				launchedProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(applicationPath);
				launchedProcess.StartInfo.Arguments = commandLine.ToString();
				launchedProcess.StartInfo.FileName = applicationPath;
				launchedProcess.Start();
			}

			return true;
		}

		/// <summary>Reads "-name value" pairs, skipping the executable name.</summary>
		public static Dictionary<string, string> ParseCommandLine(string[] commandLineArguments)
		{
			Dictionary<string, string> launchArguments = new Dictionary<string, string>();
			for (int argumentIndex = 1; argumentIndex + 1 < commandLineArguments.Length; argumentIndex++)
			{
				string argumentText = commandLineArguments[argumentIndex];
				if (argumentText.StartsWith("-") && argumentText.Length > 1)
				{
					launchArguments[argumentText.Substring(1)] = commandLineArguments[argumentIndex + 1];
					argumentIndex++;
				}
			}

			return launchArguments;
		}
	}

	/// <summary>Files under Application.persistentDataPath, for platforms with a normal file system.</summary>
	internal static class PersistentDataFiles
	{
		public static bool ReadText(string fileName, out string fileText)
		{
			fileText = null;
			string filePath = ResolvePath(fileName);
			if (filePath == null)
			{
				return false;
			}

			try
			{
				fileText = File.ReadAllText(filePath);
				return true;
			}
			catch (Exception exception)
			{
				UDebug.LogError($"Failed to read file: {filePath}\nException: {exception.Message}");
				return false;
			}
		}

		public static bool ReadBytes(string fileName, out byte[] fileBytes)
		{
			fileBytes = null;
			string filePath = ResolvePath(fileName);
			if (filePath == null)
			{
				return false;
			}

			try
			{
				fileBytes = File.ReadAllBytes(filePath);
				return true;
			}
			catch (Exception exception)
			{
				UDebug.LogError($"Failed to read file: {filePath}\nException: {exception.Message}");
				return false;
			}
		}

		public static bool WriteBytes(string fileName, byte[] fileBytes)
		{
			if (string.IsNullOrEmpty(fileName) || fileBytes == null || fileBytes.Length == 0)
			{
				UDebug.LogError("A file name and data are needed to write a file.");
				return false;
			}

			string filePath = Path.Combine(Application.persistentDataPath, fileName);
			try
			{
				File.WriteAllBytes(filePath, fileBytes);
				UDebug.Log($"File saved successfully at: {filePath}");
				return true;
			}
			catch (Exception exception)
			{
				UDebug.LogError($"Failed to write file at {filePath}:\n{exception.Message}");
				return false;
			}
		}

		public static bool WriteText(string fileName, string fileText, Encoding textEncoding)
		{
			if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileText))
			{
				UDebug.LogError("A file name and text are needed to write a file.");
				return false;
			}

			string filePath = Path.Combine(Application.persistentDataPath, fileName);
			try
			{
				File.WriteAllText(filePath, fileText, textEncoding ?? new UTF8Encoding(false));
				UDebug.Log($"File saved successfully at: {filePath}");
				return true;
			}
			catch (Exception exception)
			{
				UDebug.LogError($"Failed to write file at {filePath}:\n{exception.Message}");
				return false;
			}
		}

		/// <summary>The full path of an existing file, or null after logging why.</summary>
		private static string ResolvePath(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				UDebug.LogError("File name is null or empty.");
				return null;
			}

			string filePath = Path.Combine(Application.persistentDataPath, fileName);
			if (!File.Exists(filePath))
			{
				UDebug.LogError($"File not found at path: {filePath}");
				return null;
			}

			return filePath;
		}
	}
}
