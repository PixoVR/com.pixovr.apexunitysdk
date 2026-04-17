using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex
{
    public class ApexSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static readonly string MASTER_TAG = "ApexSingleton";
        private static T instance;

        private static readonly object lockObject = new object();

        public bool InitializeInstance(T targetInstance)
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    instance = targetInstance;
                    DontDestroyOnLoad(instance);
                    return true;
                }
                else if(instance != targetInstance)
                {
                    Debug.unityLogger.Log(LogType.Warning, MASTER_TAG, "Destroying an instance that was created after.");
                    Destroy(targetInstance.gameObject);
                }
            }

            return false;
        }

        public static T Instance
        {
            get
            {
                if (ApplicationIsQuitting)
                {
                    Debug.unityLogger.Log(LogType.Warning, MASTER_TAG, "Singleton Instance '{0}' won't be created while application is quitting.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
#if UNITY_2020_1_OR_NEWER
                        Object[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
#else
                        Object[] existingInstances = FindObjectsOfType(typeof(T));
#endif

                        if (existingInstances.Length <= 0)
                        {
                            GameObject singleton = new GameObject();
                            instance = singleton.AddComponent<T>();
                            instance.name = "Singleton_" + typeof(T).ToString();

                            DontDestroyOnLoad(instance);
                            return instance;
                        }

                        instance = existingInstances[0] as T;

                        if (existingInstances.Length > 1)
                        {
                            Debug.unityLogger.Log(LogType.Error, MASTER_TAG, "Multiple instances of '{0}' found. There should only be 1 instances. Reopening the scene might fix the problem.");
                            return instance;
                        }

                        if (instance == null)
                        {
                            Debug.unityLogger.Log(LogType.Assert, MASTER_TAG, "An instance was found and is still null.");
                            return null;
                        }
                    }

                    return instance;
                }
            }
        }

        protected static bool ApplicationIsQuitting = false;
        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public virtual void OnDestroy()
        {
            Debug.unityLogger.Log(LogType.Log, MASTER_TAG, "On Destroy on singleton called.");
            ApplicationIsQuitting = true;
        }
    }
}
