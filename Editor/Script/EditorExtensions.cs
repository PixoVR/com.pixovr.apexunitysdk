#if UNITY_EDITOR
using UnityEditor;

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
#endif