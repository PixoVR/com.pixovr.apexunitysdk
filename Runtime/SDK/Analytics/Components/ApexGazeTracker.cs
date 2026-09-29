using System;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    public sealed class ApexGazeTracker : MonoBehaviour
    {
        [SerializeField]
        private ApexGazeSource source;

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
        private string activeEngagement;
        private string currentEngagement;

        private void Awake()
        {
            ResolveSource();
        }

        private void Start()
        {
            ResolveSource();
        }

        private void Update()
        {
            if (Time.time < nextSample)
                return;
            nextSample = Time.time + Mathf.Max(0.01f, sampleInterval);
            Sample();
        }

        internal void Sample()
        {
            ResolveSource();
            if (source == null)
            {
                EndCurrent();
                currentTarget = null;
                currentEngagement = null;
                return;
            }

            Ray ray;
            string engagement;
            if (!source.TryGetGaze(out ray, out engagement) ||
                string.IsNullOrEmpty(engagement))
            {
                EndCurrent();
                currentTarget = null;
                currentEngagement = null;
                return;
            }

            RaycastHit hit;
            ApexTrackedObject target = Physics.Raycast(ray, out hit, maxDistance, layerMask)
                ? hit.collider.GetComponentInParent<ApexTrackedObject>()
                : null;

            if (target != currentTarget ||
                !string.Equals(engagement, currentEngagement, StringComparison.Ordinal))
            {
                EndCurrent();
                currentTarget = target;
                currentEngagement = engagement;
                targetSince = Time.time;
                return;
            }

            if (currentTarget != null && !gazeActive &&
                Time.time - targetSince >= Mathf.Max(0f, dwellSeconds))
            {
                currentTarget.BeginEngagement(engagement);
                gazeActive = true;
                activeEngagement = engagement;
            }
        }

        private void OnDisable()
        {
            EndCurrent();
            currentTarget = null;
            currentEngagement = null;
        }

        private void EndCurrent()
        {
            if (currentTarget != null && gazeActive)
                currentTarget.EndEngagement(activeEngagement);

            gazeActive = false;
            activeEngagement = null;
        }

        private void ResolveSource()
        {
            if (source == null)
                source = GetComponent<ApexGazeSource>();
            if (source == null)
                source = gameObject.AddComponent<ApexHeadGazeSource>();
        }
    }
}
