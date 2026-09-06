using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Alchemy.Editor;
using NUnit.Framework;
#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class GUIHelperTest
    {
        [UnityTest]
        public IEnumerator ScheduleAdjustLabelWidth_ReattachStillUpdatesLabel()
        {
            var field = new IntegerField("Value") { value = 1 };
            var window = EditModeEditorTestUtility.ShowInWindow(field);
            try
            {
                GUIHelper.ScheduleAdjustLabelWidth(field);
                var visualTree = field.panel.visualTree;
                window.position = new Rect(0f, 0f, 800f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 800f) < 1f))
                    yield return wait;

                var label = field.Q<Label>();
                Assert.That(label, Is.Not.Null);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(label.resolvedStyle.width - GUIHelper.CalculateLabelWidth(field, visualTree)) < 1f))
                    yield return wait;
                Assert.That(label.resolvedStyle.width, Is.GreaterThan(0f));
                var originalWidth = label.resolvedStyle.width;

                field.RemoveFromHierarchy();
                window.position = new Rect(0f, 0f, 500f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 500f) < 1f))
                    yield return wait;

                window.rootVisualElement.Add(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    label.resolvedStyle.width < originalWidth))
                    yield return wait;

                Assert.That(label.resolvedStyle.width, Is.LessThan(originalWidth));
                Assert.That(label.resolvedStyle.width,
                    Is.EqualTo(GUIHelper.CalculateLabelWidth(field, field.panel.visualTree)).Within(1f));

                window.position = new Rect(0f, 0f, 600f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 600f) < 1f &&
                    Mathf.Abs(label.resolvedStyle.width - GUIHelper.CalculateLabelWidth(field, visualTree)) < 1f))
                    yield return wait;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator ScheduleAdjustLabelWidth_DoesNotKeepDetachedElementAlive()
        {
            var window = EditModeEditorTestUtility.ShowInWindow(new VisualElement());
            try
            {
                var weak = CreateDetachedField(window.rootVisualElement);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return !weak.IsAlive;
                }))
                    yield return wait;

                Assert.That(weak.IsAlive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference CreateDetachedField(VisualElement root)
        {
            var field = new IntegerField("Value") { value = 1 };
            root.Add(field);
            GUIHelper.ScheduleAdjustLabelWidth(field);
            field.RemoveFromHierarchy();
            return new WeakReference(field);
        }
    }
}
