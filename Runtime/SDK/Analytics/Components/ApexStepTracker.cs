using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexStepTracker : MonoBehaviour
    {
        [SerializeField]
        private string stepName;

        public void Begin()
        {
            ApexSteps.BeginStep(stepName);
        }

        public void Complete(float score = 1f)
        {
            ApexSteps.CompleteStep(stepName, score);
        }

        public void Fail(float score = 0f)
        {
            ApexSteps.FailStep(stepName, score);
        }
    }
}
