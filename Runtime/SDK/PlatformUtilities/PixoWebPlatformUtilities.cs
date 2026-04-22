using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
    internal class PixoWebPlatformUtilities : PixoGenericPlatformUtilities
    {
        public PixoWebPlatformUtilities() : base()
        {
            UDebug.Log($"Initializing class {GetType().Name}");
        }

        public override bool OpenURL(string url)
        {
            UDebug.Log($"[{GetType().Name}] Opening url {url}");
            if (string.IsNullOrEmpty(url))
            {
                UDebug.Log("[{GetType().Name}] Url is empty or null.");
                return false;
            }

            Application.OpenURL(url);
            return true;
        }

        public override bool OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            UDebug.LogAssertion("[{GetType().Name}] Cannot open an application by path on Web.");
            return false;
        }

        public override Dictionary<string, string> ParseApplicationArguments()
        {
#if UNITY_EDITOR
            // We don't want to parse any parameters in the editor as it has its own list of arguments.
            return new Dictionary<string, string>();
#else
            UDebug.Log("[{GetType().Name}] Parsing from URL.");
            return ParseURLArguments(Application.absoluteURL);
#endif
        }

        public override bool ReadFileAsString(string fileName, out string data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UDebug.LogError("File name is null or empty.");
                data = null;
                return false;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                if (!File.Exists(path))
                {
                    UDebug.LogError($"File not found at path: {path}");
                    data = null;
                    return false;
                }

                data = File.ReadAllText(path);
                return true;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"Failed to read file: {path}\nException: {ex.Message}");
                data = null;
                return false;
            }
        }

        public override bool ReadFile(string fileName, out byte[] data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UDebug.LogError("File name is null or empty.");
                data = null;
                return false;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                if (!File.Exists(path))
                {
                    UDebug.LogError($"File not found at path: {path}");
                    data = null;
                    return false;
                }

                data = File.ReadAllBytes(path);
                return true;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"Failed to read file: {path}\nException: {ex.Message}");
                data = null;
                return false;
            }
        }

        public override bool WriteFile(string fileName, byte[] data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UDebug.LogError("File name is null or empty.");
                data = null;
                return false;
            }

            if (data == null || data.Length == 0)
            {
                UDebug.LogError("No data provided to write.");
                return false;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                File.WriteAllBytes(path, data);
                UDebug.Log($"File saved successfully at: {path}");
                return true;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"Failed to write file at {path}:\n{ex.Message}");
                return false;
            }
        }

        public override bool WriteStringToFile(string fileName, string data, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UDebug.LogError("File name is null or empty.");
                data = null;
                return false;
            }

            if (data == null || data.Length == 0)
            {
                UDebug.LogError("No data provided to write.");
                return false;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                if(encoding == null)
                {
                    File.WriteAllText(path, data);
                }
                else
                {
                    File.WriteAllText(path, data, encoding);
                }

                UDebug.Log($"File saved successfully at: {path}");
                return true;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"Failed to write file at {path}:\n{ex.Message}");
                return false;
            }
        }
    }
}