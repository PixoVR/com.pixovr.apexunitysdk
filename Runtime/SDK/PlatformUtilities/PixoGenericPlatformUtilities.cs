using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex
{
    internal class PixoGenericPlatformUtilities
    {
        public PixoGenericPlatformUtilities() { }

        public virtual bool OpenURL(string url)
        {
            return false;
        }

        public virtual bool OpenApplication(string applicationName, string[] argumentKeys, string[] argumentValues)
        {
            return false;
        }

        public virtual Dictionary<string, string> ParseApplicationArguments()
        {
            return null;
        }

        public virtual Dictionary<string, string> ParseURLArguments(string url)
        {
            Debug.Log($"Parsing URL Arguments {url}");
            string urlData = url.Substring(url.IndexOf('?') + 1);

            if (urlData.Length <= 0)
            {
                Debug.Log("No ? found in the url.");
                return null;
            }

            string[] dataArray = urlData.Split('&');

            if (dataArray.Length <= 0)
            {
                Debug.Log("No arguments found on the url.");
                return null;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (string dataElement in dataArray)
            {
                Debug.Log($"Parsing: {dataElement}");
                string[] dataParts = dataElement.Split('=', 2);

                if (dataParts.Length <= 1)
                {
                    Debug.Log($"No named argument.");
                    continue;
                }

                Debug.Log($"Found argument {dataParts[0]} - {dataParts[1]}");
                parameters.Add(dataParts[0], dataParts[1]);
            }

            return parameters;
        }

        public virtual bool ReadFileAsString(string fileName, out string data)
        {
            data = null;
            return false;
        }

        public virtual bool ReadFile(string fileName, out byte[] data)
        {
            data = null;
            return false;
        }

        public virtual bool WriteFile(string fileName, byte[] data)
        {
            return false;
        }

        public virtual bool WriteStringToFile(string fileName, string data, System.Text.Encoding encoding = null)
        {
            return false;
        }
    }
}