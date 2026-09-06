using System.Collections;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class RequiredInDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        static readonly string InstanceInSceneErrorMessage =
            RequiredMessage(nameof(RequiredInHost.instanceInScene));

        static readonly string AlwaysErrorMessage =
            RequiredMessage(nameof(RequiredInHost.always));

        const string PrefabAssetErrorMessage = "EventManager is required.";

        [TearDown]
        public void TearDown() => helper.Dispose();

        [Test]
        public void Attribute_ExposesPrefabKindAndMessage()
        {
            var unnamed = new RequiredInAttribute(PrefabKind.InstanceInScene);
            var named = new RequiredInAttribute(PrefabKind.PrefabAsset, PrefabAssetErrorMessage);

            Assert.That(unnamed.PrefabKind, Is.EqualTo(PrefabKind.InstanceInScene));
            Assert.That(unnamed.Message, Is.Null);
            Assert.That(named.PrefabKind, Is.EqualTo(PrefabKind.PrefabAsset));
            Assert.That(named.Message, Is.EqualTo(PrefabAssetErrorMessage));
        }

        [Test]
        public void Drawer_HidesHelpBoxOnNonPrefabSceneObjectWhenOnlyInstancesAreRequired()
        {
            helper.ShowInspector(CreateSceneHost());

            AssertRequiredInHelpBoxes(DisplayStyle.None, DisplayStyle.None, DisplayStyle.Flex);
        }

        [Test]
        public void Drawer_ShowsHelpBoxOnPrefabAssetWhenPrefabAssetIsRequired()
        {
            helper.ShowInspector(CreateAssetHost("_AlchemyRequiredInAsset"));

            AssertRequiredInHelpBoxes(DisplayStyle.None, DisplayStyle.Flex, DisplayStyle.Flex);
        }

        [Test]
        public void Drawer_ShowsHelpBoxOnScenePrefabInstanceWhenInstanceInSceneIsRequired()
        {
            helper.ShowInspector(CreateInstanceHost("_AlchemyRequiredInInstance"));

            AssertRequiredInHelpBoxes(DisplayStyle.Flex, DisplayStyle.None);
        }

        [UnityTest]
        public IEnumerator Drawer_HidesHelpBoxWhenRequiredReferenceIsAssigned()
        {
            helper.ShowInspector(CreateInstanceHost("_AlchemyRequiredInAssign"));
            yield return null;

            var helpBox = helper.FindHelpBox(InstanceInSceneErrorMessage);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            var serializedObject = helper.Editor.serializedObject;
            serializedObject.FindProperty(nameof(RequiredInHost.instanceInScene)).objectReferenceValue =
                helper.Create("Assigned");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;
        }

        RequiredInHost CreateSceneHost() => helper.CreateHost<RequiredInHost>();

        RequiredInHost CreateAssetHost(string prefix) =>
            helper.CreatePrefabAsset(CreateSceneHost().gameObject, prefix)
                .GetComponent<RequiredInHost>();

        RequiredInHost CreateInstanceHost(string prefix) =>
            helper.InstantiatePrefab(CreateAssetHost(prefix).gameObject)
                .GetComponent<RequiredInHost>();

        void AssertRequiredInHelpBoxes(
            DisplayStyle instanceInScene,
            DisplayStyle prefabAsset,
            DisplayStyle? always = null)
        {
            Assert.That(
                helper.FindHelpBox(InstanceInSceneErrorMessage).style.display.value,
                Is.EqualTo(instanceInScene));
            Assert.That(
                helper.FindHelpBox(PrefabAssetErrorMessage).style.display.value,
                Is.EqualTo(prefabAsset));
            if (always.HasValue)
            {
                Assert.That(
                    helper.FindHelpBox(AlwaysErrorMessage).style.display.value,
                    Is.EqualTo(always.Value));
            }
        }

        static string RequiredMessage(string fieldName) =>
            ObjectNames.NicifyVariableName(fieldName) + " is required.";
    }
}
