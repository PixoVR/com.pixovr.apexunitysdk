using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public abstract class ApexGazeSource : MonoBehaviour
    {
        // false => no gaze this frame (tracker ends current gaze)
        public abstract bool TryGetGaze(out Ray ray, out string engagement);
    }
}
