using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex
{
    public class ApexSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        private static readonly object lockObject = new object();

        public static T Instance
        {
            get
            {
                if (ApplicationIsQuitting)
                {
                    Debug.LogWarning("[ApexSingleton] Singleton Instance '{0}' won't be created while application is quitting.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        Object[] existingInstances = FindObjectsOfType(typeof(T));

                        if(existingInstances.Length <= 0)
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
                            Debug.LogError("[ApexSingleton] Multiple instances of '{0}' found. There should only be 1 instances. Reopening the scene might fix the problem.");
                            return instance;
                        }

                        if (instance == null)
                        {
                            Debug.LogAssertion("[ApexSingleton] An instance was found and is still null.");
                            return null;
                        }
                    }

                    return instance;
                }
            }
        }

        private static bool ApplicationIsQuitting = false;
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
            ApplicationIsQuitting = true;
        }
    }
}
