using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixoVR.Editor
{
    internal static class EditorExtensions
    {
        internal static SerializedProperty FindBackingFieldProperty(this SerializedObject serializedObject, string originalPropertyName)
        {
            return serializedObject.FindProperty($"<{originalPropertyName}>k__BackingField");
        }
    }
}