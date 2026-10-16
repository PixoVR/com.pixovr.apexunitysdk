using UnityEngine;

namespace PixoVR.Apex
{
	/// <summary>
	/// A MonoBehaviour with one instance per app: the first one initialized wins, later ones are
	/// destroyed, and <see cref="Instance"/> finds or creates it.
	/// </summary>
	public class ApexSingleton<TInstance> : MonoBehaviour where TInstance : MonoBehaviour
	{
		private const string kLogTag = "ApexSingleton";
		private static readonly object kInstanceLock = new object();
		private static TInstance m_instance;

		/// <summary>
		/// Set when the instance is destroyed. Unity destroys objects in an arbitrary order on quit, and
		/// creating a new instance then would leave a ghost object in the editor scene.
		/// </summary>
		protected static bool ApplicationIsQuitting { get; private set; }

		public static TInstance Instance
		{
			get
			{
				if (ApplicationIsQuitting)
				{
					Debug.unityLogger.Log(LogType.Warning, kLogTag,
						$"Singleton '{typeof(TInstance)}' won't be created while the application is quitting.");
					return null;
				}

				lock (kInstanceLock)
				{
					if (m_instance != null)
					{
						return m_instance;
					}

#if UNITY_2020_1_OR_NEWER
					TInstance[] existingInstances = FindObjectsByType<TInstance>(FindObjectsSortMode.None);
#else
					TInstance[] existingInstances = (TInstance[])FindObjectsOfType(typeof(TInstance));
#endif
					if (existingInstances.Length == 0)
					{
						GameObject singletonObject = new GameObject("Singleton_" + typeof(TInstance));
						m_instance = singletonObject.AddComponent<TInstance>();
						DontDestroyOnLoad(m_instance);
						return m_instance;
					}

					m_instance = existingInstances[0];
					if (existingInstances.Length > 1)
					{
						Debug.unityLogger.Log(LogType.Error, kLogTag,
							$"Multiple instances of '{typeof(TInstance)}' found. There should only be one.");
					}

					return m_instance;
				}
			}
		}

		/// <summary>Makes targetInstance the instance, or destroys it when another one already is.</summary>
		public bool InitializeInstance(TInstance targetInstance)
		{
			lock (kInstanceLock)
			{
				if (m_instance == null)
				{
					m_instance = targetInstance;
					DontDestroyOnLoad(m_instance);
					return true;
				}

				if (m_instance != targetInstance)
				{
					Debug.unityLogger.Log(LogType.Warning, kLogTag, "Destroying an instance that was created after.");
					Destroy(targetInstance.gameObject);
				}
			}

			return false;
		}

		public virtual void OnDestroy()
		{
			if (m_instance == this)
			{
				ApplicationIsQuitting = true;
			}
		}
	}
}
