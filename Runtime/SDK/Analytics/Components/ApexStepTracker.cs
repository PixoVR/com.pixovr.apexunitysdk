using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	public sealed class ApexStepTracker : MonoBehaviour
	{
		[SerializeField, FormerlySerializedAs("stepName")]
		private string m_stepName;

		public void Begin()
		{
			ApexSteps.BeginStep(m_stepName);
		}

		public void Complete(float score = 1f)
		{
			ApexSteps.CompleteStep(m_stepName, score);
		}

		public void Fail(float score = 0f)
		{
			ApexSteps.FailStep(m_stepName, score);
		}
	}
}
