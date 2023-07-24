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
        int selectedIndex = 0;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EndpointDisplayAttribute displayAttribute = attribute as EndpointDisplayAttribute;

            if (displayAttribute.enumDisplayList.Count > 0)
            {
                selectedIndex = EditorGUI.Popup(position, property.name, selectedIndex, displayAttribute.enumDisplayList.ToArray());
                property.enumValueIndex = selectedIndex;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
#endif
}
