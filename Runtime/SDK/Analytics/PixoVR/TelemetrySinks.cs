using System;
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PixoVR.Apex.Analytics.PixoVR
{
    public interface IApexTelemetrySink
    {
        void Send(TelemetryPacket packet, Action<bool> done);
    }

    internal static class TelemetryJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string Serialize(TelemetryPacket packet)
        {
            return JsonConvert.SerializeObject(packet, Formatting.None, Settings);
        }
    }

    public sealed class LogTelemetrySink : IApexTelemetrySink
    {
        public void Send(TelemetryPacket packet, Action<bool> done)
        {
            Debug.Log(TelemetryJson.Serialize(packet));
            done?.Invoke(true);
        }
    }

    public sealed class FileTelemetrySink : IApexTelemetrySink
    {
        private readonly string directory;

        public FileTelemetrySink(string directory)
        {
            this.directory = string.IsNullOrEmpty(directory)
                ? Path.Combine(Application.persistentDataPath, "ApexTelemetry")
                : directory;
        }

        public void Send(TelemetryPacket packet, Action<bool> done)
        {
            bool success = false;
            try
            {
                Directory.CreateDirectory(directory);
                string registration = string.IsNullOrEmpty(packet.Registration)
                    ? "nosession"
                    : packet.Registration;
                string path = Path.Combine(
                    directory,
                    string.Format("{0}-{1:D5}.json", registration, packet.Sequence));
                File.WriteAllText(path, TelemetryJson.Serialize(packet), new UTF8Encoding(false));
                success = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[PixoVRAnalytics] Failed to write telemetry: " + exception.Message);
            }

            done?.Invoke(success);
        }
    }

    public sealed class HttpTelemetrySink : IApexTelemetrySink
    {
        private readonly string url;
        private readonly Func<string> tokenProvider;

        public HttpTelemetrySink(string url, Func<string> tokenProvider)
        {
            this.url = url;
            this.tokenProvider = tokenProvider;
        }

        public void Send(TelemetryPacket packet, Action<bool> done)
        {
            PixoVRTelemetryRunner.StartRequest(SendPacket(packet, done));
        }

        private IEnumerator SendPacket(TelemetryPacket packet, Action<bool> done)
        {
            byte[] body = Encoding.UTF8.GetBytes(TelemetryJson.Serialize(packet));
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                string token = tokenProvider == null ? null : tokenProvider();
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                }

                yield return request.SendWebRequest();
                done?.Invoke(request.result == UnityWebRequest.Result.Success);
            }
        }
    }
}
