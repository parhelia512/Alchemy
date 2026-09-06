using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class SceneObjectsOnlyDrawerTest
    {
        sealed class TestWindow : EditorWindow { }

        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        readonly List<string> createdAssetPaths = new List<string>();
        EditorWindow window;
        UnityEditor.Editor editor;
        VisualElement inspectorRoot;

        static string SceneObjectErrorMessage =>
            SceneObjectsOnlyValidation.DefaultErrorMessage("Scene Object");

        [TearDown]
        public void TearDown()
        {
            CloseInspector();
            DestroyCreated();
            DeleteCreatedAssets();
        }

        [Test]
        public void Attribute_ExposesMessageDefault()
        {
            var unnamed = new SceneObjectsOnlyAttribute();
            var named = new SceneObjectsOnlyAttribute("Must be a scene object.");

            Assert.That(unnamed.Message, Is.Null);
            Assert.That(named.Message, Is.EqualTo("Must be a scene object."));
            Assert.That(
                SceneObjectsOnlyValidation.DefaultErrorMessage("Scene Object"),
                Is.EqualTo("Scene Object must be a scene object."));
        }

        [Test]
        public void Validation_AcceptsNull()
        {
            Assert.That(SceneObjectsOnlyValidation.IsValid(null), Is.True);
        }

        [Test]
        public void Validation_AcceptsSceneGameObjectAndComponent()
        {
            var host = CreateHost();
            var other = Create("Other");

            Assert.That(SceneObjectsOnlyValidation.IsValid(host.gameObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(host), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(other), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(other.transform), Is.True);
        }

        [Test]
        public void Validation_RejectsAssetsAndNonSceneObjects()
        {
            var host = CreateHost();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            var transient = Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());

            Assert.That(SceneObjectsOnlyValidation.IsValid(Texture2D.whiteTexture), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(prefab), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(transient), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(host.gameObject), Is.True);
        }

        [Test]
        public void Validation_ResolvesNestedFields()
        {
            var host = CreateHost();
            host.nested.sceneObject = Create("Nested");

            using var serializedObject = new SerializedObject(host);
            var property = serializedObject.FindProperty("nested.sceneObject");

            Assert.That(property, Is.Not.Null);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(property), Is.True);

            host.nested.sceneObject = CreatePrefabAsset(Create("PrefabSource"));
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(property), Is.False);
            Assert.That(host.nested.sceneObject, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(host.nested.sceneObject), Is.True);
        }

        [Test]
        public void Validation_ValidatesArrayElementsAndReportsUnsupportedUse()
        {
            var host = CreateHost();
            host.sceneObjects = new[] { Create("A"), (GameObject)null };
            host.unsupported = 1;
            host.texture = Texture2D.whiteTexture;

            using var serializedObject = new SerializedObject(host);
            var sceneObjects = serializedObject.FindProperty("sceneObjects");
            var unsupported = serializedObject.FindProperty("unsupported");
            var texture = serializedObject.FindProperty("texture");
            var anyObject = serializedObject.FindProperty("anyObject");

            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(sceneObjects), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(anyObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(texture), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(sceneObjects), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(texture), Is.False);

            host.sceneObjects[0] = CreatePrefabAsset(Create("PrefabSource"));
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(sceneObjects), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(unsupported), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(unsupported), Is.False);
        }

        [Test]
        public void Validation_MixedMultiObjectSelectionIsInvalidWhenAnyTargetFails()
        {
            var (host1, host2) = TwoHosts();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            host1.sceneObject = Create("SceneA");
            host2.sceneObject = prefab;
            host1.sceneObjects = new[] { host1.sceneObject };
            host2.sceneObjects = new[] { host2.sceneObject };

            using var serializedObject = Multi(host1, host2);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            var sceneObjects = serializedObject.FindProperty("sceneObjects");

            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.False);

            host2.sceneObject = Create("SceneB");
            host2.sceneObjects = new[] { host2.sceneObject };
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.True);
        }

        [Test]
        public void Validation_PendingMultiObjectAssetMustBeInvalidForEveryTarget()
        {
            var (hostA, hostB) = TwoHosts();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));

            using var serializedObject = Multi(hostA, hostB);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            sceneObject.objectReferenceValue = prefab;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.sceneObject, Is.Null);
            Assert.That(hostB.sceneObject, Is.Null);
            Assert.That(sceneObject.objectReferenceValue, Is.SameAs(prefab));
        }

        [Test]
        public void Validation_AcceptsValidPendingChangeWithoutApplying()
        {
            var host = CreateHost();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            var scene = Create("Scene");
            host.sceneObject = prefab;
            host.sceneObjects = new[] { prefab };

            using var serializedObject = new SerializedObject(host);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            var sceneObjects = serializedObject.FindProperty("sceneObjects");
            sceneObject.objectReferenceValue = scene;
            sceneObjects.GetArrayElementAtIndex(0).objectReferenceValue = scene;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(host.sceneObject, Is.SameAs(prefab));
            Assert.That(host.sceneObjects[0], Is.SameAs(prefab));
            Assert.That(sceneObject.objectReferenceValue, Is.SameAs(scene));
            Assert.That(sceneObjects.GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(scene));
        }

        [Test]
        public void Validation_SupportsScriptableObjectTargets()
        {
            var asset = Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());
            var scene = Create("Scene");
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            asset.sceneObject = scene;

            using var serializedObject = new SerializedObject(asset);
            var property = serializedObject.FindProperty("sceneObject");

            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(property), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(property), Is.True);

            property.objectReferenceValue = prefab;
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(property), Is.False);
            Assert.That(asset.sceneObject, Is.SameAs(scene));
        }

        [UnityTest]
        public IEnumerator Drawer_ShowsErrorHelpBoxWhilePreservingInvalidValue()
        {
            var host = CreateHost();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            host.sceneObject = prefab;
            ShowInspector(host);
            yield return null;

            var helpBox = FindHelpBox(SceneObjectErrorMessage);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;
            Assert.That(host.sceneObject, Is.SameAs(prefab));

            var scene = Create("Scene");
            var serializedObject = editor.serializedObject;
            serializedObject.FindProperty("sceneObject").objectReferenceValue = scene;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.sceneObject, Is.SameAs(scene));
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            var customHelpBox = FindHelpBox("Must be a scene object.");
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                yield return wait;

            serializedObject.FindProperty("sceneTransform").objectReferenceValue = prefab.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.sceneTransform, Is.SameAs(prefab.transform));
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                yield return wait;

            var unsupportedHelpBoxes = inspectorRoot.Query<HelpBox>().ToList()
                .Where(box => box.messageType == HelpBoxMessageType.Warning)
                .ToList();
            Assert.That(unsupportedHelpBoxes, Is.Not.Empty);
            Assert.That(
                unsupportedHelpBoxes.All(box => box.text.Contains("SceneObjectsOnly can only be used")),
                Is.True);
        }

        [Test]
        public void Drawer_ShowsErrorForMixedMultiObjectSelection()
        {
            var (host1, host2) = TwoHosts();
            var prefab = CreatePrefabAsset(Create("PrefabSource"));
            host1.sceneObject = Create("SceneA");
            host2.sceneObject = prefab;
            ShowInspector(host1, host2);

            Assert.That(FindHelpBox(SceneObjectErrorMessage).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host1.sceneObject, Is.Not.Null);
            Assert.That(host2.sceneObject, Is.SameAs(prefab));
        }

        [Test]
        public void Drawer_ValidatesScriptableObjectTargetsInsteadOfUnsupported()
        {
            var asset = Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());
            asset.sceneObject = CreatePrefabAsset(Create("PrefabSource"));
            CreateInspector(asset);

            var helpBoxes = inspectorRoot.Query<HelpBox>().ToList();
            Assert.That(helpBoxes.Any(box => box.messageType == HelpBoxMessageType.Warning), Is.False);
            var error = helpBoxes.FirstOrDefault(box => box.messageType == HelpBoxMessageType.Error);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.text, Is.EqualTo(SceneObjectErrorMessage));
            Assert.That(error.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Validation_RejectsPrefabAssetsAndAcceptsPrefabStageObjects()
        {
            var path = $"Assets/_AlchemySceneObjectsOnly_{Guid.NewGuid():N}.prefab";
            GameObject contents = null;
            var openedStage = false;
            try
            {
                var host = CreateHost();
                Assert.That(PrefabUtility.SaveAsPrefabAsset(host.gameObject, path), Is.Not.Null);
                createdAssetPaths.Add(path);
                DestroyCreated();

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(asset, Is.Not.Null);
                Assert.That(EditorUtility.IsPersistent(asset), Is.True);
                Assert.That(SceneObjectsOnlyValidation.IsValid(asset), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(asset.GetComponent<SceneObjectsOnlyHost>()), Is.False);

                var sceneInstance = Track(PrefabUtility.InstantiatePrefab(asset) as GameObject);
                Assert.That(EditorUtility.IsPersistent(sceneInstance), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(sceneInstance), Is.True);

                contents = PrefabUtility.LoadPrefabContents(path);
                Assert.That(EditorUtility.IsPersistent(contents), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(contents), Is.True);

                var stage = PrefabStageUtility.OpenPrefab(path);
                if (stage == null)
                    return;

                openedStage = true;
                Assert.That(SceneObjectsOnlyValidation.IsValid(stage.prefabContentsRoot), Is.True);
            }
            finally
            {
                if (openedStage)
                    StageUtility.GoToMainStage();
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        SceneObjectsOnlyHost CreateHost(string name = "Owner")
        {
            var owner = new GameObject(name);
            var host = owner.AddComponent<SceneObjectsOnlyHost>();
            host.nested = new SceneObjectsOnlyHost.Nested();
            Track(owner);
            return host;
        }

        (SceneObjectsOnlyHost hostA, SceneObjectsOnlyHost hostB) TwoHosts() =>
            (CreateHost("OwnerA"), CreateHost("OwnerB"));

        GameObject Create(string name) => Track(new GameObject(name));

        GameObject CreatePrefabAsset(GameObject source)
        {
            var path = $"Assets/_AlchemySceneObjectsOnly_{Guid.NewGuid():N}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(source, path);
            createdAssetPaths.Add(path);
            return asset;
        }

        T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        static SerializedObject Multi(params UnityEngine.Object[] targets) => new SerializedObject(targets);

        void ShowInspector(params UnityEngine.Object[] targets)
        {
            CreateInspector(targets);
            window = ScriptableObject.CreateInstance<TestWindow>();
            window.position = new Rect(0f, 0f, 640f, 480f);
            window.rootVisualElement.Add(inspectorRoot);
            window.Show();
        }

        void CreateInspector(params UnityEngine.Object[] targets)
        {
            editor = UnityEditor.Editor.CreateEditor(targets);
            inspectorRoot = editor.CreateInspectorGUI();
        }

        void CloseInspector()
        {
            if (inspectorRoot != null)
            {
                inspectorRoot.Unbind();
                inspectorRoot.RemoveFromHierarchy();
                inspectorRoot = null;
            }

            if (window != null)
            {
                window.Close();
                if (window != null)
                    UnityEngine.Object.DestroyImmediate(window);
                window = null;
            }

            if (editor != null)
            {
                UnityEngine.Object.DestroyImmediate(editor);
                editor = null;
            }
        }

        void DestroyCreated()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    UnityEngine.Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        void DeleteCreatedAssets()
        {
            for (var i = createdAssetPaths.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(createdAssetPaths[i]))
                    AssetDatabase.DeleteAsset(createdAssetPaths[i]);
            }
            createdAssetPaths.Clear();
        }

        static IEnumerable WaitUntilDisplay(HelpBox helpBox, DisplayStyle expected, float timeoutSeconds = 2f)
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

        HelpBox FindHelpBox(string text)
        {
            var helpBox = inspectorRoot.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.text == text);
            Assert.That(helpBox, Is.Not.Null, $"Expected HelpBox '{text}'.");
            return helpBox;
        }
    }
}
