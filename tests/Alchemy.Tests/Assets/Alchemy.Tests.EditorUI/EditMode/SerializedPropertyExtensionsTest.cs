using System;
using System.Collections.Generic;
using Alchemy.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class SerializedPropertyExtensionsTest
    {
        [Serializable]
        public class NestedItem
        {
            public int value;
        }

        class ListHost : ScriptableObject
        {
            public List<NestedItem> items;
        }

        class ArrayHost : ScriptableObject
        {
            public NestedItem[] items;
        }

        [Test]
        public void GetValue_ListOfClass_ReadsIndexedElement()
        {
            var host = ScriptableObject.CreateInstance<ListHost>();
            try
            {
                host.items = new List<NestedItem>(50);
                for (var i = 0; i < 50; i++)
                {
                    host.items.Add(new NestedItem { value = i });
                }

                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty("items.Array.data[42].value");

                Assert.That(property, Is.Not.Null);
                Assert.That(property.GetValue<int>(), Is.EqualTo(42));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetValue_ArrayOfClass_ReadsIndexedElement()
        {
            var host = ScriptableObject.CreateInstance<ArrayHost>();
            try
            {
                host.items = new NestedItem[20];
                for (var i = 0; i < host.items.Length; i++)
                {
                    host.items[i] = new NestedItem { value = i * 3 };
                }

                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty("items.Array.data[17].value");

                Assert.That(property, Is.Not.Null);
                Assert.That(property.GetValue<int>(), Is.EqualTo(51));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetValue_ListOfClass_ReadsFirstAndLastElements()
        {
            var host = ScriptableObject.CreateInstance<ListHost>();
            try
            {
                host.items = new List<NestedItem>
                {
                    new NestedItem { value = 11 },
                    new NestedItem { value = 22 },
                    new NestedItem { value = 33 },
                };

                var serializedObject = new SerializedObject(host);
                Assert.That(
                    serializedObject.FindProperty("items.Array.data[0].value").GetValue<int>(),
                    Is.EqualTo(11));
                Assert.That(
                    serializedObject.FindProperty("items.Array.data[2].value").GetValue<int>(),
                    Is.EqualTo(33));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
