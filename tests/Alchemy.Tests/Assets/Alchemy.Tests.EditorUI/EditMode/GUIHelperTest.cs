using System;
using System.Collections;
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
                yield return null;

                var label = field.Q<Label>();
                Assert.That(label, Is.Not.Null);
                Assert.That(label.resolvedStyle.width, Is.GreaterThan(0f));

                field.RemoveFromHierarchy();
                yield return null;

                window.rootVisualElement.Add(field);
                yield return null;

                Assert.That(label.resolvedStyle.width, Is.GreaterThan(0f));
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
            WeakReference weak;
            try
            {
                {
                    var field = new IntegerField("Value") { value = 1 };
                    window.rootVisualElement.Add(field);
                    GUIHelper.ScheduleAdjustLabelWidth(field);
                    yield return null;
                    weak = new WeakReference(field);
                    field.RemoveFromHierarchy();
                }

                yield return null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                yield return null;

                Assert.That(weak.IsAlive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
