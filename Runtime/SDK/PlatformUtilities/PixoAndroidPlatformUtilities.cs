using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace PixoVR.Apex
{
    internal class PixoAndroidPlatformUtilities : PixoGenericPlatformUtilities
    {
        // TODO: Migrate functionality from the old PixoAndroidUtils into this class.
        public PixoAndroidPlatformUtilities() : base()
        {

        }

        public override bool OpenURL(string url)
        {
            return PixoAndroidUtils.LaunchUrl(url);
        }

        public override bool OpenApplication(string applicationPath, string[] argumentKeys, string[] argumentValues)
        {
            return PixoAndroidUtils.LaunchApp(applicationPath, argumentKeys, argumentValues);
        }

        public override Dictionary<string, string> ParseApplicationArguments()
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            string urlData = intent.Call<string>("getDataString");

            string optionalParameter = "", returnTargetParameter = "", targetTypeParameter = "", pixotoken = "";

            UDebug.Log("[PixoAndroidPlatformUtilities] Parsed Passed Data.");
            if(urlData != null && urlData.Length > 0)
            {
                UDebug.Log("[PixoAndroidPlatformUtilities] Parse from URL.");
                return ParseURLArguments(urlData);
            }
            else
            {
                // TODO: Loop through ALL extras and return them
                UDebug.Log("[PixoAndroidPlatformUtilities] Parsing from extras.");
                optionalParameter = intent.Call<string>("getStringExtra", "optional");
                returnTargetParameter = intent.Call<string>("getStringExtra", "returntarget");
                targetTypeParameter = intent.Call<string>("getStringExtra", "targettype");
                pixotoken = intent.Call<string>("getStringExtra", "pixotoken");
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(optionalParameter))
            {
                parameters.Add("optional", optionalParameter);
            }

            if (!string.IsNullOrEmpty(returnTargetParameter))
            {
                parameters.Add("returntarget", returnTargetParameter);
            }

            if (!string.IsNullOrEmpty(targetTypeParameter))
            {
                parameters.Add("targettype", targetTypeParameter);
            }

            if (!string.IsNullOrEmpty(pixotoken))
            {
                parameters.Add("pixotoken", pixotoken);
            }

            return parameters;
        }
    }
}