using System;
using UnityEngine;

namespace PixoVR.Apex
{
    public class PixoSingleton<T> where T : class, new()
    {
        private readonly static T instance = new T();

        public static T Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
