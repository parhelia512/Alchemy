using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class ChildObjectsOnlyValidation
    {
        public static bool IsSupportedReferenceType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(UnityEngine.Object)) return true;
            if (type == typeof(GameObject)) return true;
            return typeof(Component).IsAssignableFrom(type);
        }

        public static bool IsSupportedProperty(SerializedProperty property) =>
            SerializedObjectReferenceValidation.IsSupportedProperty(property, IsSupportedReferenceType);

        public static Transform GetOwnerTransform(UnityEngine.Object target)
        {
            if (target is Component component && component != null)
            {
                return component.transform;
            }

            if (target is GameObject gameObject && gameObject != null)
            {
                return gameObject.transform;
            }

            return null;
        }

        public static Transform GetOwnerTransform(SerializedObject serializedObject)
        {
            try
            {
                return GetOwnerTransform(serializedObject?.targetObject);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string DefaultErrorMessage(string displayName, bool includeSelf)
        {
            var name = ObjectNames.NicifyVariableName(displayName);
            return includeSelf
                ? name + " must be this GameObject, a descendant, or a component on those objects."
                : name + " must be a descendant GameObject or a component on a descendant.";
        }

        public static bool IsValid(UnityEngine.Object value, Transform owner, bool includeSelf)
        {
            if (value == null) return true;
            if (owner == null) return false;
            if (!IsSceneHierarchyObject(value)) return false;

            var gameObject = GetGameObject(value);
            if (gameObject == null) return false;

            var transform = gameObject.transform;
            if (!transform.IsChildOf(owner)) return false;
            if (!includeSelf && transform == owner) return false;
            return true;
        }

        public static bool IsSerializedPropertyValid(SerializedProperty property, bool includeSelf) =>
            SerializedObjectReferenceValidation.IsSerializedPropertyValid(
                property,
                target =>
                {
                    var owner = GetOwnerTransform(target);
                    return value => IsValid(value, owner, includeSelf);
                });

        public static bool IsPropertyValid(SerializedProperty property, Transform owner, bool includeSelf) =>
            SerializedObjectReferenceValidation.IsPropertyValid(
                property,
                value => IsValid(value, owner, includeSelf));

        public static bool TryAccessProperty(
            SerializedProperty property,
            out SerializedObject serializedObject,
            out string path) =>
            SerializedObjectReferenceValidation.TryAccessProperty(property, out serializedObject, out path);

        public static bool IsSceneHierarchyObject(UnityEngine.Object value)
        {
            var gameObject = GetGameObject(value);
            if (gameObject == null) return false;
            if (EditorUtility.IsPersistent(value) || EditorUtility.IsPersistent(gameObject))
            {
                return false;
            }

            return gameObject.scene.IsValid();
        }

        public static GameObject GetGameObject(UnityEngine.Object value)
        {
            switch (value)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }
    }
}
