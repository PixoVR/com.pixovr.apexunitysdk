using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixoVR.Apex
{
    public class EndpointDisplayAttribute : PropertyAttribute
    {
        public List<string> enumDisplayList = new List<string>();
        public List<PlatformServer> enumList = new List<PlatformServer>();

        public EndpointDisplayAttribute()
        {
            enumList = Enum.GetValues(typeof(PlatformServer)).Cast<PlatformServer>().ToList();

            foreach(PlatformServer enumItem in enumList)
            {
                enumDisplayList.Add(enumItem.ToDisplayString());
            }
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EndpointDisplayAttribute))]
    public class EndpointDisplayDrawer : PropertyDrawer
    {
        [SerializeField]
        int selectedIndex = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var displayAttribute = (EndpointDisplayAttribute)attribute;
            if (displayAttribute.enumDisplayList.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex,
                displayAttribute.enumDisplayList.ToArray());
            if (EditorGUI.EndChangeCheck())
                property.enumValueIndex = newIndex;
            EditorGUI.EndProperty();
        }
    }
#endif
}
