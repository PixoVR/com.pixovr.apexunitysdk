using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PixoVR.Apex
{
	/// <summary>Shows an EPlatformServer field as a list of environment display names.</summary>
	public class EndpointDisplayAttribute : PropertyAttribute
	{
		public EndpointDisplayAttribute()
		{
			PlatformServers = Enum.GetValues(typeof(EPlatformServer)).Cast<EPlatformServer>().ToList();
			DisplayNames = PlatformServers.Select(platformServer => platformServer.ToDisplayString()).ToList();
		}

		public IReadOnlyList<EPlatformServer> PlatformServers { get; }
		public IReadOnlyList<string> DisplayNames { get; }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EndpointDisplayAttribute))]
	public class EndpointDisplayDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EndpointDisplayAttribute displayAttribute = (EndpointDisplayAttribute)attribute;
			if (displayAttribute.DisplayNames.Count == 0)
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			int selectedIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex,
				displayAttribute.DisplayNames.ToArray());
			if (EditorGUI.EndChangeCheck())
			{
				property.enumValueIndex = selectedIndex;
			}

			EditorGUI.EndProperty();
		}
	}
#endif
}
