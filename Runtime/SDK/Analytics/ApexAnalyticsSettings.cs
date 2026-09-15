using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.Apex.Analytics
{
    [CreateAssetMenu(fileName = "ApexAnalyticsSettings", menuName = "PixoVR/Apex Analytics Settings")]
    public class ApexAnalyticsSettings : ScriptableObject
    {
        [SerializeField]
        private bool analyticsEnabled = true;

        [SerializeField]
        private List<string> disabledProviders = new();

        public bool AnalyticsEnabled => analyticsEnabled;
        public IReadOnlyList<string> DisabledProviders => disabledProviders;

        public static ApexAnalyticsSettings Load()
        {
            return Resources.Load<ApexAnalyticsSettings>("ApexAnalyticsSettings");
        }
    }
}
