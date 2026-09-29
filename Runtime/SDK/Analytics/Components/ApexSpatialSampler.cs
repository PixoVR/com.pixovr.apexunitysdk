using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexSpatialSampler : MonoBehaviour
    {
        public static Transform Head { get; private set; }

        private void OnEnable()
        {
            Head = transform;
        }

        private void OnDisable()
        {
            if (Head == transform)
                Head = null;
        }
    }
}
