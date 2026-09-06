using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    internal static class EditModeEditorTestUtility
    {
        sealed class TestWindow : EditorWindow { }

        public static EditorWindow ShowInWindow(VisualElement content)
        {
            var window = ScriptableObject.CreateInstance<TestWindow>();
            window.position = new Rect(0f, 0f, 640f, 480f);
            window.rootVisualElement.Add(content);
            window.Show();
            return window;
        }

        public static IEnumerable WaitUntil(Func<bool> condition, float timeoutSeconds = 2f)
        {
            var deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                    "Timed out waiting for the Editor UI to update.");
                yield return null;
            }
        }
    }
}
