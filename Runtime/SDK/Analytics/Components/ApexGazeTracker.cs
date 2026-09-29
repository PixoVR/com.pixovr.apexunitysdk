using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexGazeTracker : MonoBehaviour
    {
        [SerializeField]
        private Camera camera;

        [SerializeField]
        private float maxDistance = 10f;

        [SerializeField]
        private float dwellSeconds = 0.5f;

        [SerializeField]
        private float sampleInterval = 0.1f;

        [SerializeField]
        private LayerMask layerMask = ~0;

        private ApexTrackedObject currentTarget;
        private float targetSince;
        private float nextSample;
        private bool gazeActive;

        private void Start()
        {
            if (camera == null)
                camera = Camera.main;
        }

        private void Update()
        {
            if (camera == null || Time.time < nextSample)
                return;

            nextSample = Time.time + Mathf.Max(0.01f, sampleInterval);
            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            RaycastHit hit;
            ApexTrackedObject target = Physics.Raycast(ray, out hit, maxDistance, layerMask)
                ? hit.collider.GetComponentInParent<ApexTrackedObject>()
                : null;

            if (target != currentTarget)
            {
                EndCurrent();
                currentTarget = target;
                targetSince = Time.time;
                return;
            }

            if (currentTarget != null && !gazeActive &&
                Time.time - targetSince >= Mathf.Max(0f, dwellSeconds))
            {
                currentTarget.BeginEngagement("gaze");
                gazeActive = true;
            }
        }

        private void OnDisable()
        {
            EndCurrent();
            currentTarget = null;
        }

        private void EndCurrent()
        {
            if (currentTarget != null && gazeActive)
                currentTarget.EndEngagement("gaze");

            gazeActive = false;
        }
    }
}
