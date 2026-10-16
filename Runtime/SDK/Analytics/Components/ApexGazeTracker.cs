using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	public sealed class ApexGazeTracker : MonoBehaviour
	{
		[SerializeField, FormerlySerializedAs("source")]
		private ApexGazeSource m_source;

		[SerializeField, FormerlySerializedAs("maxDistance")]
		private float m_maximumDistance = 10f;

		[SerializeField, FormerlySerializedAs("dwellSeconds")]
		private float m_dwellSeconds = 0.5f;

		[SerializeField, FormerlySerializedAs("sampleInterval")]
		private float m_sampleIntervalSeconds = 0.1f;

		[SerializeField, FormerlySerializedAs("layerMask")]
		private LayerMask m_layerMask = ~0;

		private ApexTrackedObject m_currentTarget;
		private float m_targetSinceSeconds;
		private float m_nextSampleSeconds;
		private bool m_gazeActive;
		private string m_activeEngagement;
		private string m_currentEngagement;

		private void Start()
		{
			ResolveSource();
		}

		private void Update()
		{
			if (Time.time < m_nextSampleSeconds)
			{
				return;
			}

			m_nextSampleSeconds = Time.time + Mathf.Max(0.01f, m_sampleIntervalSeconds);
			Sample();
		}

		internal void Sample()
		{
			ResolveSource();
			if (m_source == null)
			{
				EndCurrent();
				m_currentTarget = null;
				m_currentEngagement = null;
				return;
			}

			Ray ray;
			string engagement;
			if (!m_source.TryGetGaze(out ray, out engagement) ||
				string.IsNullOrEmpty(engagement))
			{
				EndCurrent();
				m_currentTarget = null;
				m_currentEngagement = null;
				return;
			}

			RaycastHit hit;
			ApexTrackedObject target = Physics.Raycast(ray, out hit, m_maximumDistance, m_layerMask)
				? hit.collider.GetComponentInParent<ApexTrackedObject>()
				: null;

			if (target != m_currentTarget ||
				!string.Equals(engagement, m_currentEngagement, StringComparison.Ordinal))
			{
				EndCurrent();
				m_currentTarget = target;
				m_currentEngagement = engagement;
				m_targetSinceSeconds = Time.time;
				return;
			}

			if (m_currentTarget != null && !m_gazeActive &&
				Time.time - m_targetSinceSeconds >= Mathf.Max(0f, m_dwellSeconds))
			{
				m_currentTarget.BeginEngagement(engagement);
				m_gazeActive = true;
				m_activeEngagement = engagement;
			}
		}

		private void OnDisable()
		{
			EndCurrent();
			m_currentTarget = null;
			m_currentEngagement = null;
		}

		private void EndCurrent()
		{
			if (m_currentTarget != null && m_gazeActive)
			{
				m_currentTarget.EndEngagement(m_activeEngagement);
			}

			m_gazeActive = false;
			m_activeEngagement = null;
		}

		private void ResolveSource()
		{
			if (m_source == null)
			{
				m_source = GetComponent<ApexGazeSource>();
			}

			if (m_source == null)
			{
				m_source = gameObject.AddComponent<ApexHeadGazeSource>();
			}
		}
	}
}
