using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using Alchemy.Tests.EditorUI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class RequiredListLengthDrawerTest
    {
        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        EditorWindow window;
        UnityEditor.Editor editor;
        VisualElement inspectorRoot;

        [TearDown]
        public void TearDown()
        {
            CloseInspector();
            DestroyCreated();
        }

        [Test]
        public void Attribute_ExposesExactAndRangeBounds()
        {
            var exact = new RequiredListLengthAttribute(1);
            var range = new RequiredListLengthAttribute(0, 10);
            var maxOnly = new RequiredListLengthAttribute(null, 10);
            var minOnly = new RequiredListLengthAttribute(10, null);
            var custom = new RequiredListLengthAttribute(1) { Message = "Must have exactly one item." };

            Assert.That(exact.Min, Is.EqualTo(1));
            Assert.That(exact.Max, Is.EqualTo(1));

            Assert.That(range.Min, Is.EqualTo(0));
            Assert.That(range.Max, Is.EqualTo(10));
            Assert.That(maxOnly.Min, Is.Null);
            Assert.That(maxOnly.Max, Is.EqualTo(10));
            Assert.That(minOnly.Min, Is.EqualTo(10));
            Assert.That(minOnly.Max, Is.Null);
            Assert.That(custom.Message, Is.EqualTo("Must have exactly one item."));
        }

        [Test]
        public void Attribute_RejectsInvalidBoundsWithoutThrowing()
        {
            Assert.DoesNotThrow(() => new RequiredListLengthAttribute(-1));
            Assert.DoesNotThrow(() => new RequiredListLengthAttribute(null, null));
            Assert.DoesNotThrow(() => new RequiredListLengthAttribute(10, 1));
            Assert.DoesNotThrow(() => new RequiredListLengthAttribute("foo", 10));

            Assert.That(new RequiredListLengthAttribute(-1).Min, Is.Null);
            Assert.That(new RequiredListLengthAttribute(-1).Max, Is.Null);
            Assert.That(new RequiredListLengthAttribute(null, null).Min, Is.Null);
            Assert.That(new RequiredListLengthAttribute(10, 1).Min, Is.Null);
            Assert.That(new RequiredListLengthAttribute("foo", 10).Max, Is.Null);
            Assert.That(new RequiredListLengthAttribute(0).Min, Is.EqualTo(0));
            Assert.That(new RequiredListLengthAttribute(0).Max, Is.EqualTo(0));
        }

        [Test]
        public void Validation_DefaultMessagesCoverExactMinMaxAndRange()
        {
            Assert.That(
                RequiredListLengthValidation.DefaultMessage("exact", 1, 1),
                Is.EqualTo("Exact must contain exactly 1 element."));
            Assert.That(
                RequiredListLengthValidation.DefaultMessage("range", 2, 10),
                Is.EqualTo("Range must contain between 2 and 10 elements."));
            Assert.That(
                RequiredListLengthValidation.DefaultMessage("atMost", null, 1),
                Is.EqualTo("At Most must contain at most 1 element."));
            Assert.That(
                RequiredListLengthValidation.DefaultMessage("atLeast", 10, null),
                Is.EqualTo("At Least must contain at least 10 elements."));
        }

        [Test]
        public void Validation_AcceptsSizesInsideBounds()
        {
            Assert.That(RequiredListLengthValidation.IsValid(1, 1, 1), Is.True);
            Assert.That(RequiredListLengthValidation.IsValid(0, 1, 1), Is.False);
            Assert.That(RequiredListLengthValidation.IsValid(2, 1, 1), Is.False);
            Assert.That(RequiredListLengthValidation.IsValid(0, 0, 2), Is.True);
            Assert.That(RequiredListLengthValidation.IsValid(2, 0, 2), Is.True);
            Assert.That(RequiredListLengthValidation.IsValid(3, 0, 2), Is.False);
            Assert.That(RequiredListLengthValidation.IsValid(2, null, 2), Is.True);
            Assert.That(RequiredListLengthValidation.IsValid(3, null, 2), Is.False);
            Assert.That(RequiredListLengthValidation.IsValid(2, 2, null), Is.True);
            Assert.That(RequiredListLengthValidation.IsValid(1, 2, null), Is.False);
        }

        [Test]
        public void Validation_SupportsArraysAndListsAndRejectsOtherFields()
        {
            var host = CreateHost();
            host.exact = new int[1];
            host.range = new List<int> { 1 };
            host.atMost = new[] { 1, 2, 3 };
            host.atLeast = new List<string>();

            using var serializedObject = new SerializedObject(host);
            Assert.That(
                RequiredListLengthValidation.IsSupportedProperty(serializedObject.FindProperty("exact")),
                Is.True);
            Assert.That(
                RequiredListLengthValidation.IsSupportedProperty(serializedObject.FindProperty("range")),
                Is.True);
            Assert.That(
                RequiredListLengthValidation.IsSupportedProperty(serializedObject.FindProperty("unsupported")),
                Is.False);
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(serializedObject.FindProperty("exact"), 1, 1),
                Is.True);
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(serializedObject.FindProperty("atMost"), null, 2),
                Is.False);
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(serializedObject.FindProperty("atLeast"), 2, null),
                Is.False);
            Assert.That(host.atMost, Has.Length.EqualTo(3));
            Assert.That(host.atLeast, Is.Empty);
        }

        [Test]
        public void Validation_PendingSingleObjectSizeChangeDoesNotApply()
        {
            var host = CreateHost();
            host.exact = Array.Empty<int>();

            using var serializedObject = new SerializedObject(host);
            var exact = serializedObject.FindProperty("exact");
            exact.arraySize = 1;

            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(exact, 1, 1),
                Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(host.exact, Is.Empty);
        }

        [Test]
        public void Validation_PendingUniformSizeUsesSharedArraySizeWhenElementsDiffer()
        {
            var hostA = CreateHost("OwnerA");
            var hostB = CreateHost("OwnerB");
            hostA.exact = new[] { 1, 2 };
            hostB.exact = new[] { 3, 4 };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var exact = serializedObject.FindProperty("exact");
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(exact, 1, 1),
                Is.False);

            exact.arraySize = 1;
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(exact, 1, 1),
                Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.exact, Has.Length.EqualTo(2));
            Assert.That(hostB.exact, Has.Length.EqualTo(2));
        }

        [Test]
        public void Validation_MixedSizesValidateEachAppliedTarget()
        {
            var hostA = CreateHost("OwnerA");
            var hostB = CreateHost("OwnerB");
            hostA.exact = new[] { 1 };
            hostB.exact = Array.Empty<int>();

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var exact = serializedObject.FindProperty("exact");
            Assert.That(
                RequiredListLengthValidation.IsSerializedPropertyValid(exact, 1, 1),
                Is.False);
            Assert.That(hostA.exact, Has.Length.EqualTo(1));
            Assert.That(hostB.exact, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Drawer_ShowsErrorHelpBoxWhilePreservingInvalidSize()
        {
            var host = CreateHost();
            host.exact = new int[0];
            host.custom = new int[0];
            ShowInspector(host);
            yield return null;

            var exactHelpBox = FindHelpBox("Exact must contain exactly 1 element.");
            foreach (var wait in WaitUntilDisplay(exactHelpBox, DisplayStyle.Flex))
                yield return wait;
            Assert.That(host.exact, Is.Empty);

            var serializedObject = editor.serializedObject;
            serializedObject.FindProperty("exact").arraySize = 1;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.exact, Has.Length.EqualTo(1));
            foreach (var wait in WaitUntilDisplay(exactHelpBox, DisplayStyle.None))
                yield return wait;

            serializedObject.FindProperty("exact").arraySize = 2;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.exact, Has.Length.EqualTo(2));
            foreach (var wait in WaitUntilDisplay(exactHelpBox, DisplayStyle.Flex))
                yield return wait;

            var customHelpBox = FindHelpBox("Must have exactly one item.");
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                yield return wait;

            serializedObject.FindProperty("custom").arraySize = 1;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.custom, Has.Length.EqualTo(1));
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                yield return wait;

            var unsupportedHelpBoxes = inspectorRoot.Query<HelpBox>().ToList()
                .Where(box => box.messageType == HelpBoxMessageType.Warning)
                .Where(box => box.text.Contains("RequiredListLength can only be used"))
                .ToList();
            Assert.That(unsupportedHelpBoxes, Is.Not.Empty);

            var invalidBoundsHelpBoxes = inspectorRoot.Query<HelpBox>().ToList()
                .Where(box => box.text == RequiredListLengthValidation.InvalidBoundsMessage)
                .ToList();
            Assert.That(invalidBoundsHelpBoxes, Is.Not.Empty);
        }

        [Test]
        public void Drawer_ShowsErrorForMixedMultiObjectSelection()
        {
            var host1 = CreateHost("OwnerA");
            var host2 = CreateHost("OwnerB");
            host1.exact = new int[1];
            host2.exact = new int[0];
            ShowInspector(host1, host2);

            Assert.That(
                FindHelpBox("Exact must contain exactly 1 element.").style.display.value,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host1.exact, Has.Length.EqualTo(1));
            Assert.That(host2.exact, Is.Empty);
        }

        RequiredListLengthHost CreateHost(string name = "Owner")
        {
            var owner = Track(new GameObject(name));
            return owner.AddComponent<RequiredListLengthHost>();
        }

        T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        void ShowInspector(params UnityEngine.Object[] targets)
        {
            CreateInspector(targets);
            window = EditModeEditorTestUtility.ShowInWindow(inspectorRoot);
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

        static IEnumerable WaitUntilDisplay(HelpBox helpBox, DisplayStyle expected)
        {
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(
                         () => helpBox.style.display.value == expected))
            {
                yield return wait;
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
