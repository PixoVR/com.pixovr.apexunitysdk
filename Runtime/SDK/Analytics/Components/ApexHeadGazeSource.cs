using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexHeadGazeSource : ApexGazeSource
    {
        [SerializeField]
        private Transform head;

        [SerializeField]
        private string engagementName = "gaze";

        public override bool TryGetGaze(out Ray ray, out string engagement)
        {
            Transform gazeTransform = head != null
                ? head
                : ApexSpatialSampler.Head != null ? ApexSpatialSampler.Head : transform;
            ray = new Ray(gazeTransform.position, gazeTransform.forward);
            engagement = engagementName;
            return true;
        }
    }
}
