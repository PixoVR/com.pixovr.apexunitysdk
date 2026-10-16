using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	public sealed class ApexHeadGazeSource : ApexGazeSource
	{
		[SerializeField, FormerlySerializedAs("head")]
		private Transform m_head;

		[SerializeField, FormerlySerializedAs("engagementName")]
		private string m_engagementName = "gaze";

		public override bool TryGetGaze(out Ray ray, out string engagement)
		{
			Transform gazeTransform = m_head != null
				? m_head
				: ApexSpatialSampler.Head != null ? ApexSpatialSampler.Head : transform;
			ray = new Ray(gazeTransform.position, gazeTransform.forward);
			engagement = m_engagementName;
			return true;
		}
	}
}
