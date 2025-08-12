using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex
{
    internal class PixoGenericPlatformUtilities
    {
        public PixoGenericPlatformUtilities()
        {
            Debug.Log($"Initializing class {GetType().Name}");
        }

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

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // We need to parse out the optional parameter, since it can contain &.
            string[] optionalDataSplit = urlData.Split("optional=");
            if (optionalDataSplit.Length > 1)
            {
                Debug.Log($"Optional data parts {optionalDataSplit[0]}  --  {optionalDataSplit[1]}");
                string optionalData = optionalDataSplit[1];
                int jsonEndPosition = optionalData.LastIndexOf('}');
                string isolatedOptionalData = optionalData.Substring(0, jsonEndPosition + 1);
                Debug.Log($"Isolated data: {isolatedOptionalData}");
                parameters.Add("optional", isolatedOptionalData);

                if(jsonEndPosition < optionalData.Length - 1)
                {
                    string otherData = optionalData.Substring(jsonEndPosition + 1);
                    Debug.Log($"Other data: {otherData}");
                    string baseData = optionalDataSplit[0];
                    if (baseData.Length > 0 && baseData.EndsWith('&'))
                    {
                        baseData = baseData.Remove(baseData.Length - 1, 1);
                    }
                    urlData = baseData + otherData;
                    Debug.Log($"Combined url: {urlData}");
                }
            }

            string[] dataArray = urlData.Split('&');

            if (dataArray.Length <= 0)
            {
                Debug.Log("No arguments found on the url.");
                return null;
            }

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