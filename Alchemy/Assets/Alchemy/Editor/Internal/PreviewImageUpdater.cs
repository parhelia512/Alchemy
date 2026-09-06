using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    internal sealed class PreviewImageUpdater
    {
        public const int DefaultMaxAttempts = 64;

        readonly VisualElement scheduler;
        readonly Image image;
        readonly Func<UnityEngine.Object, Texture> getPreview;
        readonly int maxAttempts;

        IVisualElementScheduledItem job;
        UnityEngine.Object target;
        int attempts;

        public PreviewImageUpdater(
            VisualElement scheduler,
            Image image,
            Func<UnityEngine.Object, Texture> getPreview = null,
            int maxAttempts = DefaultMaxAttempts)
        {
            this.scheduler = scheduler;
            this.image = image;
            this.getPreview = getPreview ?? AssetPreview.GetAssetPreview;
            this.maxAttempts = maxAttempts;
        }

        public bool HasActiveJob => job != null;
        public int Attempts => attempts;
        public UnityEngine.Object Target => target;

        public void Update(UnityEngine.Object reference)
        {
            if (reference == null)
            {
                Stop();
                image.image = null;
                target = null;
                attempts = 0;
                return;
            }

            if (reference != target)
            {
                Stop();
                image.image = null;
                target = reference;
                attempts = 0;
            }

            if (image.image != null) return;
            if (job != null) return;
            if (attempts >= maxAttempts) return;

            var captured = reference;
            job = scheduler.schedule.Execute(() =>
            {
                if (image.panel == null)
                {
                    Stop();
                    return;
                }

                if (captured != target) return;

                attempts++;
                image.image = getPreview(captured);
                if (image.image != null || attempts >= maxAttempts)
                {
                    job = null;
                }
            }).Until(() =>
                image.panel == null
                || image.image != null
                || attempts >= maxAttempts
                || captured != target
            );
        }

        public void Stop()
        {
            job?.Pause();
            job = null;
        }
    }
}
