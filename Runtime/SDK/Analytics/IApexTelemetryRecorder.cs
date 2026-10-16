using UnityEngine;

namespace PixoVR.Apex.Analytics
{
	/// <summary>
	/// Where the PixoVR provider sends what the player does. The core client implements it; the
	/// core numbers objects, times and batches the records, and sends the packets.
	/// </summary>
	internal interface IApexTelemetryRecorder
	{
		void TelemetryObjectRegistered(string trackedId, string objectName, string meshName, Vector3 position,
			Vector3 rotation);
		void TelemetryObjectUnregistered(string trackedId, Vector3 position, Vector3 rotation);
		void TelemetryEngagementBegin(string trackedId, string engagementName);
		void TelemetryEngagementEnd(string trackedId, string engagementName);
		void TelemetryInteraction(string trackedId, string actionName);
		void TelemetryStepBegin(string stepName);
		void TelemetryStepEnd(string stepName, bool succeeded, double score);
		/// <summary>True when the head and tracked objects should report their poses this frame.</summary>
		bool TelemetryPoseDue();
		/// <summary>A null or empty tracked ID is the head.</summary>
		void TelemetryPose(string trackedId, Vector3 position, Vector3 rotation);
		void TelemetryFlush();
	}
}
