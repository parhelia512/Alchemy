using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    sealed class ObjectReferenceValidationTestHelper : IDisposable
    {
        sealed class TestWindow : EditorWindow { }

        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        readonly List<string> createdAssetPaths = new List<string>();

        public EditorWindow Window { get; private set; }
        public UnityEditor.Editor Editor { get; private set; }
        public VisualElement InspectorRoot { get; private set; }

        public void Dispose()
        {
            CloseInspector();
            DestroyCreated();
            DeleteCreatedAssets();
        }

        public THost CreateHost<THost>(string name = "Owner") where THost : MonoBehaviour
        {
            var owner = new GameObject(name);
            var host = owner.AddComponent<THost>();
            Track(owner);
            return host;
        }

        public GameObject CreateChild(Component parent, string name) => CreateChild(parent.gameObject, name);

        public GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return Track(child);
        }

        public GameObject Create(string name) => Track(new GameObject(name));

        public GameObject CreatePrefabAsset(GameObject source, string prefix = "_AlchemyObjectReference")
        {
            var path = $"Assets/{prefix}_{Guid.NewGuid():N}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(source, path);
            createdAssetPaths.Add(path);
            return asset;
        }

        public T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        public static SerializedObject Multi(params UnityEngine.Object[] targets) => new SerializedObject(targets);

        public void ShowInspector(params UnityEngine.Object[] targets)
        {
            CreateInspector(targets);
            Window = ScriptableObject.CreateInstance<TestWindow>();
            Window.position = new Rect(0f, 0f, 640f, 480f);
            Window.rootVisualElement.Add(InspectorRoot);
            Window.Show();
        }

        public void CreateInspector(params UnityEngine.Object[] targets)
        {
            Editor = UnityEditor.Editor.CreateEditor(targets);
            InspectorRoot = Editor.CreateInspectorGUI();
        }

        public void CloseInspector()
        {
            if (InspectorRoot != null)
            {
                InspectorRoot.Unbind();
                InspectorRoot.RemoveFromHierarchy();
                InspectorRoot = null;
            }

            if (Window != null)
            {
                Window.Close();
                if (Window != null)
                    UnityEngine.Object.DestroyImmediate(Window);
                Window = null;
            }

            if (Editor != null)
            {
                UnityEngine.Object.DestroyImmediate(Editor);
                Editor = null;
            }
        }

        public void DestroyCreated()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    UnityEngine.Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        public void DeleteCreatedAssets()
        {
            for (var i = createdAssetPaths.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(createdAssetPaths[i]))
                    AssetDatabase.DeleteAsset(createdAssetPaths[i]);
            }
            createdAssetPaths.Clear();
        }

        public static IEnumerable WaitUntilDisplay(HelpBox helpBox, DisplayStyle expected, float timeoutSeconds = 2f)
        {
            var deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (helpBox.style.display.value != expected)
            {
                if (EditorApplication.timeSinceStartup >= deadline)
                {
                    Assert.That(
                        helpBox.style.display.value,
                        Is.EqualTo(expected),
                        $"HelpBox display did not become {expected} within {timeoutSeconds} seconds.");
                    yield break;
                }

                yield return null;
            }
        }

        public HelpBox FindHelpBox(string text)
        {
            var helpBox = InspectorRoot.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.text == text);
            Assert.That(helpBox, Is.Not.Null, $"Expected HelpBox '{text}'.");
            return helpBox;
        }
    }
}
