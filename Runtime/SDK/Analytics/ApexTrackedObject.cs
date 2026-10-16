using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.Apex.Analytics
{
	[DisallowMultipleComponent]
	[AddComponentMenu("PixoVR/Apex Tracked Object")]
	public class ApexTrackedObject : MonoBehaviour
	{
		[SerializeField, FormerlySerializedAs("trackedId")]
		private string m_trackedId;

		[SerializeField, FormerlySerializedAs("displayName")]
		private string m_displayName;

		[SerializeField, FormerlySerializedAs("meshName")]
		private string m_meshName;

		[SerializeField, FormerlySerializedAs("trackPose")]
		private bool m_trackPose;

		/// <summary>
		/// Gets the per-instance id. Authored scene objects keep their serialized id across runs;
		/// spawned or duplicated instances get a fresh id when enabled.
		/// </summary>
		public string TrackedId => m_trackedId;
		public string DisplayName => string.IsNullOrEmpty(m_displayName) ? gameObject.name : m_displayName;
		public string MeshName => m_meshName;
		public bool TrackPose => m_trackPose;

		private void Awake()
		{
			EnsureTrackedId();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			EnsureTrackedId();
		}
#endif

		private void OnEnable()
		{
			EnsureUniqueTrackedId();
			ApexAnalytics.RegisterTrackedObject(this);
		}

		private void OnDisable()
		{
			ApexAnalytics.UnregisterTrackedObject(this);
		}

		public void SetTrackedId(string newTrackedId)
		{
			if (string.IsNullOrEmpty(newTrackedId))
			{
				throw new ArgumentException("Tracked id cannot be null or empty.", nameof(newTrackedId));
			}

			bool wasRegistered = isActiveAndEnabled && IsRegistered();
			if (wasRegistered)
			{
				ApexAnalytics.UnregisterTrackedObject(this);
			}

			m_trackedId = newTrackedId;
			EnsureUniqueTrackedId();

			if (wasRegistered)
			{
				ApexAnalytics.RegisterTrackedObject(this);
			}
		}

		public void BeginEngagement(string engagement)
		{
			if (string.IsNullOrEmpty(engagement))
			{
				Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot begin an empty engagement.");
				return;
			}

			ApexAnalytics.Dispatch(provider => provider.OnEngagementBegin(this, engagement));
		}

		public void EndEngagement(string engagement)
		{
			if (string.IsNullOrEmpty(engagement))
			{
				Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot end an empty engagement.");
				return;
			}

			ApexAnalytics.Dispatch(provider => provider.OnEngagementEnd(this, engagement));
		}

		public void RecordInteraction(string action)
		{
			if (string.IsNullOrEmpty(action))
			{
				Debug.unityLogger.Log(LogType.Warning, "ApexAnalytics", "Cannot record an empty interaction.");
				return;
			}

			ApexAnalytics.Dispatch(provider => provider.OnInteraction(this, action));
		}

		private void EnsureTrackedId()
		{
			if (string.IsNullOrEmpty(m_trackedId))
			{
				m_trackedId = Guid.NewGuid().ToString("N");
			}
		}

		private void EnsureUniqueTrackedId()
		{
			EnsureTrackedId();

			foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
			{
				if (trackedObject != this && trackedObject.TrackedId == m_trackedId)
				{
					m_trackedId = Guid.NewGuid().ToString("N");
					break;
				}
			}
		}

		private bool IsRegistered()
		{
			foreach (ApexTrackedObject trackedObject in ApexAnalytics.TrackedObjects)
			{
				if (trackedObject == this)
				{
					return true;
				}
			}

			return false;
		}
	}
}
