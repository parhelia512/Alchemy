using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class RequiredListLengthValidation
    {
        public const string UnsupportedMessage =
            "RequiredListLength can only be used on arrays or lists.";

        public const string InvalidBoundsMessage =
            "RequiredListLength bounds are invalid. Use a non-negative length, or a min/max pair with at least one bound.";

        public static bool IsSupportedProperty(SerializedProperty property)
        {
            return TryGetArraySize(property, out _);
        }

        public static bool IsValid(int size, int? min, int? max)
        {
            if (size < 0) return false;
            if (min.HasValue && size < min.Value) return false;
            if (max.HasValue && size > max.Value) return false;
            return true;
        }

        public static bool IsSerializedPropertyValid(SerializedProperty property, int? min, int? max)
        {
            if (!TryGetArraySize(property, out var size))
            {
                return false;
            }

            try
            {
                if (!property.hasMultipleDifferentValues)
                {
                    return IsValid(size, min, max);
                }
            }
            catch (Exception)
            {
                return IsValid(size, min, max);
            }

            var serializedObject = property.serializedObject;
            if (serializedObject?.targetObjects == null || serializedObject.targetObjects.Length <= 1)
            {
                return IsValid(size, min, max);
            }

            foreach (var target in serializedObject.targetObjects)
            {
                if (target == null) continue;

                using var individual = new SerializedObject(target);
                var individualProperty = individual.FindProperty(property.propertyPath);
                if (!TryGetArraySize(individualProperty, out var individualSize) ||
                    !IsValid(individualSize, min, max))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryGetArraySize(SerializedProperty property, out int size)
        {
            size = 0;
            if (property == null) return false;

            try
            {
                if (property.propertyType == SerializedPropertyType.String) return false;
                if (!property.isArray) return false;

                size = property.arraySize;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string DefaultMessage(string displayName, int? min, int? max)
        {
            var name = ObjectNames.NicifyVariableName(displayName);
            if (min.HasValue && max.HasValue && min == max)
            {
                return name + " must contain exactly " + min.Value + " " + Noun(min.Value) + ".";
            }

            if (min.HasValue && max.HasValue)
            {
                return name + " must contain between " + min.Value + " and " + max.Value + " elements.";
            }

            if (max.HasValue)
            {
                return name + " must contain at most " + max.Value + " " + Noun(max.Value) + ".";
            }

            if (min.HasValue)
            {
                return name + " must contain at least " + min.Value + " " + Noun(min.Value) + ".";
            }

            return name + " has an invalid list length requirement.";
        }

        static string Noun(int count) => count == 1 ? "element" : "elements";
    }
}
