using System.Collections.Generic;

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
            string urlData = url.Substring(url.IndexOf('?') + 1);

            if (urlData.Length <= 0)
                return null;

            string[] dataArray = urlData.Split('&');

            if (dataArray.Length <= 0)
                return null;

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (string dataElement in dataArray)
            {
                string[] dataParts = dataElement.Split('=', 1);

                if (dataParts.Length <= 1)
                    continue;

                parameters.Add(dataParts[0], dataParts[1]);
            }

            return parameters;
        }
    }
}