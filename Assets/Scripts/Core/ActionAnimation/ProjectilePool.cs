using System;
using PrimeTween;
using TextRPG.Core.UnitRendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.ActionAnimation
{
    internal sealed class ProjectilePool : IDisposable
    {
        private const int MaxPoolSize = 16;
        private static readonly Vector2 ProjectileSize = new(120, 50);

        private VisualElement _overlayRoot;
        private readonly System.Collections.Generic.Queue<ProjectileEntry> _available = new();
        private readonly System.Collections.Generic.HashSet<ProjectileEntry> _active = new();
        private int _instanceCount;
        private bool _disposed;

        public int ActiveCount => _active.Count;

        public static VisualElement CreateOverlay()
        {
            var overlay = new VisualElement { name = "projectile-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.pickingMode = PickingMode.Ignore;
            return overlay;
        }

        public void Initialize(VisualElement overlayRoot, int prewarmCount = 8)
        {
            _overlayRoot = overlayRoot;
            _instanceCount = 0;

            for (int i = 0; i < prewarmCount; i++)
                _available.Enqueue(CreateEntry());
        }

        public ProjectileEntry Acquire(string text, Color color, Vector3 panelPosition)
        {
            ProjectileEntry entry;
            if (_available.Count > 0)
            {
                entry = _available.Dequeue();
            }
            else
            {
                entry = CreateEntry();
            }

            _active.Add(entry);

            entry.Root.style.left = panelPosition.x;
            entry.Root.style.top = panelPosition.y;
            entry.Root.style.display = DisplayStyle.Flex;

            entry.Container.Clear();
            entry.Container.style.backgroundColor = Color.black;

            var displayText = text?.ToUpperInvariant() ?? "";
            var layout = UnitTextLayout.Calculate(displayText, ProjectileSize.x, ProjectileSize.y);
            UnitTextLabels.AddTo(layout, color, entry.Container);

            return entry;
        }

        public void Return(ProjectileEntry entry)
        {
            if (_disposed || entry.Root == null) return;

            _active.Remove(entry);

            if (entry.CurrentTween.isAlive)
                entry.CurrentTween.Stop();

            entry.Container?.Clear();
            entry.Root.style.display = DisplayStyle.None;
            entry.Root.style.left = 0;
            entry.Root.style.top = 0;
            entry.Root.style.scale = new Scale(Vector3.one);

            if (_available.Count >= MaxPoolSize)
            {
                entry.Root.RemoveFromHierarchy();
                return;
            }

            _available.Enqueue(entry);
        }

        private ProjectileEntry CreateEntry()
        {
            var root = new VisualElement();
            root.name = $"projectile_{_instanceCount++}";
            root.style.position = Position.Absolute;
            root.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            root.pickingMode = PickingMode.Ignore;
            root.style.display = DisplayStyle.None;

            var container = new VisualElement();
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 3;
            container.style.paddingBottom = 3;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            container.pickingMode = PickingMode.Ignore;
            root.Add(container);

            _overlayRoot.Add(root);

            return new ProjectileEntry
            {
                Root = root,
                Container = container
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var entry in _active)
            {
                if (entry.CurrentTween.isAlive)
                    entry.CurrentTween.Stop();
                entry.Root?.RemoveFromHierarchy();
            }
            _active.Clear();

            while (_available.Count > 0)
            {
                var entry = _available.Dequeue();
                if (entry.CurrentTween.isAlive)
                    entry.CurrentTween.Stop();
                entry.Root?.RemoveFromHierarchy();
            }
        }
    }

    internal sealed class ProjectileEntry
    {
        public VisualElement Root;
        public VisualElement Container;
        public Tween CurrentTween;
    }
}
