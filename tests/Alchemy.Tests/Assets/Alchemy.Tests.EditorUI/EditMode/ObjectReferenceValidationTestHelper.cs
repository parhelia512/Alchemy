using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        readonly List<GameObject> loadedPrefabContents = new List<GameObject>();
        bool openedPrefabStage;

        public EditorWindow Window { get; private set; }
        public UnityEditor.Editor Editor { get; private set; }
        public VisualElement InspectorRoot { get; private set; }

        public void Dispose()
        {
            CloseInspector();
            ClosePrefabStage();
            UnloadPrefabContents();
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

        public GameObject CreatePrefabAsset(string name, string prefix = "_AlchemyObjectReference") =>
            CreatePrefabAsset(Create(name), prefix);

        public GameObject CreatePrefabAsset(GameObject source, string prefix = "_AlchemyObjectReference")
        {
            var path = $"Assets/{prefix}_{Guid.NewGuid():N}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(source, path);
            createdAssetPaths.Add(path);
            return asset;
        }

        public GameObject CreatePrefabVariant(string sourceName, string prefix = "_AlchemyPrefabVariant") =>
            CreatePrefabVariant(CreatePrefabAsset(sourceName, prefix + "Base"), prefix);

        public GameObject CreatePrefabVariant(GameObject prefabAsset, string prefix = "_AlchemyPrefabVariant")
        {
            var instance = InstantiatePrefab(prefabAsset);
            var path = $"Assets/{prefix}_{Guid.NewGuid():N}.prefab";
            var variant = PrefabUtility.SaveAsPrefabAsset(instance, path);
            createdAssetPaths.Add(path);
            return variant;
        }

        public GameObject CreateNestedPrefabAsset(string prefix = "_AlchemyNestedPrefab") =>
            CreateNestedPrefabAsset(CreatePrefabAsset("Inner", prefix + "Inner"), prefix);

        public GameObject CreateNestedPrefabAsset(GameObject innerPrefab, string prefix = "_AlchemyNestedPrefab")
        {
            var outer = Create("Outer");
            InstantiatePrefab(innerPrefab).transform.SetParent(outer.transform);
            return CreatePrefabAsset(outer, prefix);
        }

        public GameObject CreateModelPrefabAsset(string prefix = "_AlchemyModel")
        {
            var folderName = $"{prefix}_{Guid.NewGuid():N}";
            var folder = $"Assets/{folderName}";
            AssetDatabase.CreateFolder("Assets", folderName);
            createdAssetPaths.Add(folder);

            var path = $"{folder}/Model.obj";
            File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
            AssetDatabase.ImportAsset(path);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(model, Is.Not.Null);
            return model;
        }

        public GameObject InstantiatePrefab(GameObject prefabAsset) =>
            Track((GameObject)PrefabUtility.InstantiatePrefab(prefabAsset));

        public GameObject LoadPrefabContents(GameObject prefabAsset)
        {
            var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefabAsset));
            loadedPrefabContents.Add(contents);
            return contents;
        }

        public PrefabStage OpenPrefab(GameObject prefabAsset)
        {
            var stage = PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefabAsset));
            if (stage != null)
                openedPrefabStage = true;
            return stage;
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

        void ClosePrefabStage()
        {
            if (!openedPrefabStage)
                return;

            StageUtility.GoToMainStage();
            openedPrefabStage = false;
        }

        void UnloadPrefabContents()
        {
            for (var i = loadedPrefabContents.Count - 1; i >= 0; i--)
            {
                if (loadedPrefabContents[i] != null)
                    PrefabUtility.UnloadPrefabContents(loadedPrefabContents[i]);
            }
            loadedPrefabContents.Clear();
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
