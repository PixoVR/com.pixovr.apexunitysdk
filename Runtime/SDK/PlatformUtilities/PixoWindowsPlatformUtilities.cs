using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
    internal class PixoWindowsPlatformUtilities : PixoGenericPlatformUtilities
    {
        public PixoWindowsPlatformUtilities() : base()
        {

        }

        public override bool OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                UDebug.Log("Url is empty or null.");
                return false;
            }

            Application.OpenURL(url);
            return true;
        }

        public override bool OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            if (!string.IsNullOrEmpty(applicationPath))
            {
                UDebug.Log("Application is empty.");
                return false;
            }

            if (!File.Exists(applicationPath))
            {
                UDebug.Log($"Application does not exist at {applicationPath}");
                return false;
            }

            int argumentCount = Mathf.Max(argumentKeys.Length, argumentValues.Length);
            if (argumentKeys.Length != argumentValues.Length)
            {
                UDebug.LogWarning("The number of argument keys and values are not equal. Extra arguments will not be provided and mapping could be messed up.");
            }

            string arguments = "";
            for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
            {
                if (argumentIndex > 0)
                    arguments += " ";

                // We use space here between the key and value as most OS's that are launched with arguments separate by spaces.
                // We also escape our values to ensure that strings containing spaces get captured.
                arguments += $"-{argumentKeys[argumentIndex]} \"{argumentValues[argumentIndex]}\"";
            }

            using (Process process = new Process())
            {
                process.StartInfo.WorkingDirectory = applicationPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.FileName = applicationPath;
                process.Start();
            }

            return true;
        }

        public override Dictionary<string, string> ParseApplicationArguments()
        {
#if UNITY_EDITOR
            // We don't want to parse any parameters in the editor as it has its own list of arguments.
            return new Dictionary<string, string>();
#else
            string[] args = System.Environment.GetCommandLineArgs();

            UDebug.Log($"First argument: {args[0]}");

            if(args.Length == 2)
            {
                string urlData = args[1];
                UDebug.Log("[PixoWindowsPlatformUtilities] Parse from URL.");
                return ParseURLArguments(urlData);
            }

            UDebug.Log("[PixoWindowsPlatformUtilities] Parsing arguments from commandline.");
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // We skip the first argument, as it's the executables name.
            for(int argumentIndex = 1; argumentIndex < args.Length; argumentIndex++)
            {
                if (args[argumentIndex].StartsWith('-') && ((argumentIndex + 1) < args.Length))
                {
                    parameters.Add(args[argumentIndex].Remove(0), args[argumentIndex + 1]);
                    argumentIndex++;
                }
            }

            return parameters;
#endif
        }
    }
}