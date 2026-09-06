using System.Collections;
using Alchemy.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class PreviewImageUpdaterTest
    {
        [UnityTest]
        public IEnumerator Update_StartsOnlyOneJobForTheSameReference()
        {
            var image = new Image();
            var host = new VisualElement();
            host.Add(image);
            var window = EditModeEditorTestUtility.ShowInWindow(host);
            var calls = 0;
            try
            {
                yield return null;

                var updater = new PreviewImageUpdater(host, image, _ =>
                {
                    calls++;
                    return null;
                }, maxAttempts: 16);
                var reference = Texture2D.whiteTexture;

                updater.Update(reference);
                updater.Update(reference);
                updater.Update(reference);
                Assert.That(updater.HasActiveJob, Is.True);

                yield return null;
                var callsAfterFirstFrame = calls;
                yield return null;

                Assert.That(updater.HasActiveJob, Is.True);
                Assert.That(callsAfterFirstFrame, Is.GreaterThan(0));
                Assert.That(calls - callsAfterFirstFrame, Is.EqualTo(1));

                updater.Stop();
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator Update_StopsJobWhenReferenceBecomesNull()
        {
            var image = new Image();
            var host = new VisualElement();
            host.Add(image);
            var window = EditModeEditorTestUtility.ShowInWindow(host);
            var calls = 0;
            try
            {
                yield return null;

                var updater = new PreviewImageUpdater(host, image, _ =>
                {
                    calls++;
                    return null;
                }, maxAttempts: 16);

                updater.Update(Texture2D.whiteTexture);
                yield return null;
                Assert.That(updater.HasActiveJob, Is.True);
                Assert.That(calls, Is.GreaterThan(0));

                updater.Update(null);
                Assert.That(image.image, Is.Null);
                Assert.That(updater.HasActiveJob, Is.False);
                Assert.That(updater.Target, Is.Null);

                var callsAfterStop = calls;
                yield return null;
                yield return null;
                Assert.That(calls, Is.EqualTo(callsAfterStop));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator Update_RestartsWhenReferenceChanges()
        {
            var image = new Image();
            var host = new VisualElement();
            host.Add(image);
            var window = EditModeEditorTestUtility.ShowInWindow(host);
            UnityEngine.Object lastRequested = null;
            try
            {
                yield return null;

                var updater = new PreviewImageUpdater(host, image, value =>
                {
                    lastRequested = value;
                    return null;
                }, maxAttempts: 16);

                updater.Update(Texture2D.whiteTexture);
                yield return null;
                Assert.That(lastRequested, Is.SameAs(Texture2D.whiteTexture));

                updater.Update(Texture2D.blackTexture);
                Assert.That(image.image, Is.Null);
                Assert.That(updater.Target, Is.SameAs(Texture2D.blackTexture));
                Assert.That(updater.HasActiveJob, Is.True);
                Assert.That(updater.Attempts, Is.EqualTo(0));

                yield return null;
                Assert.That(lastRequested, Is.SameAs(Texture2D.blackTexture));
                updater.Stop();
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator Update_StopsAfterMaxAttempts()
        {
            var image = new Image();
            var host = new VisualElement();
            host.Add(image);
            var window = EditModeEditorTestUtility.ShowInWindow(host);
            const int maxAttempts = 3;
            try
            {
                yield return null;

                var updater = new PreviewImageUpdater(host, image, _ => null, maxAttempts);
                updater.Update(Texture2D.whiteTexture);

                for (var i = 0; i < maxAttempts + 2; i++)
                {
                    yield return null;
                }

                Assert.That(updater.HasActiveJob, Is.False);
                Assert.That(updater.Attempts, Is.EqualTo(maxAttempts));
                Assert.That(image.image, Is.Null);

                updater.Update(Texture2D.whiteTexture);
                yield return null;
                Assert.That(updater.HasActiveJob, Is.False);
                Assert.That(updater.Attempts, Is.EqualTo(maxAttempts));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator Update_StopsWhenPreviewBecomesAvailable()
        {
            var image = new Image();
            var host = new VisualElement();
            host.Add(image);
            var window = EditModeEditorTestUtility.ShowInWindow(host);
            Texture preview = null;
            try
            {
                yield return null;

                var updater = new PreviewImageUpdater(host, image, _ => preview, maxAttempts: 8);
                updater.Update(Texture2D.whiteTexture);
                yield return null;
                Assert.That(updater.HasActiveJob, Is.True);

                preview = Texture2D.whiteTexture;
                yield return null;
                Assert.That(image.image, Is.SameAs(Texture2D.whiteTexture));
                Assert.That(updater.HasActiveJob, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }
    }
}
