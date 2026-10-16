using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PixoVR.Apex.Analytics.PixoVR
{
	/// <summary>
	/// Handlers for the telemetry packets the core hands back (as JSON) when the sink is LOG or
	/// FILE. With HTTP the core sends the packets itself.
	/// </summary>
	public static class TelemetryPacketWriters
	{
		private const string kLogTag = "PixoVRAnalytics";

		/// <summary>The handler for a sink type, or null when the core does not hand packets back.</summary>
		public static Action<string> Create(ETelemetrySinkType sinkType)
		{
			switch (sinkType)
			{
				case ETelemetrySinkType.LOG:
					return WriteToLog;
				case ETelemetrySinkType.FILE:
					string telemetryDirectory = Path.Combine(Application.persistentDataPath, "ApexTelemetry");
					return packetJson => WriteToFile(telemetryDirectory, packetJson);
				default:
					return null;
			}
		}

		public static void WriteToLog(string packetJson)
		{
			Debug.unityLogger.Log(LogType.Log, kLogTag, packetJson);
		}

		/// <summary>Saves the packet as &lt;session&gt;-&lt;sequence&gt;.json in the directory.</summary>
		public static void WriteToFile(string telemetryDirectory, string packetJson)
		{
			try
			{
				JObject packet = JsonReading.ParseObject(packetJson);
				string registration = JsonReading.GetString(packet, "r");
				int sequence = JsonReading.GetInteger(packet, "seq") ?? 0;
				string fileName = string.Format(CultureInfo.InvariantCulture, "{0}-{1:D5}.json",
					string.IsNullOrEmpty(registration) ? "nosession" : registration, sequence);
				Directory.CreateDirectory(telemetryDirectory);
				File.WriteAllText(Path.Combine(telemetryDirectory, fileName), packetJson, new UTF8Encoding(false));
			}
			catch (Exception exception)
			{
				Debug.unityLogger.Log(LogType.Warning, kLogTag, "Failed to write telemetry: " + exception.Message);
			}
		}
	}
}
