using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class SceneObjectsOnlyValidation
    {
        public static bool IsSupportedReferenceType(Type type) =>
            type != null && typeof(UnityEngine.Object).IsAssignableFrom(type);

        public static bool IsSupportedProperty(SerializedProperty property) =>
            SerializedObjectReferenceValidation.IsSupportedProperty(property, IsSupportedReferenceType);

        public static string DefaultErrorMessage(string displayName) =>
            ObjectNames.NicifyVariableName(displayName) + " must be a scene object.";

        public static bool IsValid(UnityEngine.Object value) =>
            value == null || ChildObjectsOnlyValidation.IsSceneHierarchyObject(value);

        public static bool IsSerializedPropertyValid(SerializedProperty property) =>
            SerializedObjectReferenceValidation.IsSerializedPropertyValid(
                property,
                _ => IsValid);

        public static bool IsPropertyValid(SerializedProperty property) =>
            SerializedObjectReferenceValidation.IsPropertyValid(property, IsValid);

        public static bool TryAccessProperty(
            SerializedProperty property,
            out SerializedObject serializedObject,
            out string path) =>
            SerializedObjectReferenceValidation.TryAccessProperty(property, out serializedObject, out path);
    }
}
