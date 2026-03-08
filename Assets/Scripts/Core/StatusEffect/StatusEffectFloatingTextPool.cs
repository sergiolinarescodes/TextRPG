using System;
using System.Collections.Generic;
using PrimeTween;
using TextRPG.Core.UnitRendering;
using Unidad.Core.UI.TextAnimation;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectFloatingTextPool : IDisposable
    {
        private const int MaxPoolSize = 12;
        private const float DriftDistance = 40f;
        private const float Duration = 0.8f;
        private static readonly Vector2 FloatingSize = new(120, 50);

        private readonly TextAnimationService _textAnimService;

        private VisualElement _overlayRoot;
        private readonly Queue<FloatingEntry> _available = new();
        private readonly HashSet<FloatingEntry> _active = new();
        private int _instanceCount;
        private bool _disposed;

        public StatusEffectFloatingTextPool(TextAnimationService textAnimService)
        {
            _textAnimService = textAnimService;
        }

        public static VisualElement CreateOverlay()
        {
            var overlay = new VisualElement { name = "floating-text-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.pickingMode = PickingMode.Ignore;
            return overlay;
        }

        public void Initialize(VisualElement overlayRoot, int prewarmCount = 6)
        {
            _overlayRoot = overlayRoot;
            _instanceCount = 0;

            for (int i = 0; i < prewarmCount; i++)
                _available.Enqueue(CreateEntry());
        }

        public void Spawn(Vector2 panelPosition, string text, Color color, string recipeName = "damage")
        {
            if (_disposed || _overlayRoot == null) return;

            FloatingEntry entry;
            if (_available.Count > 0)
            {
                entry = _available.Dequeue();
            }
            else if (_active.Count >= MaxPoolSize)
            {
                return;
            }
            else
            {
                entry = CreateEntry();
            }

            _active.Add(entry);

            var startY = panelPosition.y;
            entry.Root.style.left = panelPosition.x;
            entry.Root.style.top = startY;
            entry.Root.style.display = DisplayStyle.Flex;
            entry.Root.style.opacity = 1f;

            entry.Container.Clear();
            var displayText = text?.ToUpperInvariant() ?? "";
            var layout = UnitTextLayout.Calculate(displayText, FloatingSize.x, FloatingSize.y);
            var recipe = _textAnimService.GetRecipe(recipeName);
            if (recipe != null)
                UnitTextLabels.AddAnimatedWithRecipeTo(layout, color, entry.Container, recipe.MarkupTemplate);
            else
                UnitTextLabels.AddTo(layout, color, entry.Container);

            entry.CurrentTween = Tween.Custom(entry, 0f, 1f, Duration,
                onValueChange: (e, progress) =>
                {
                    if (e.Root == null) return;
                    e.Root.style.top = startY - DriftDistance * progress;
                    e.Root.style.opacity = progress < 0.5f ? 1f : 1f - (progress - 0.5f) * 2f;
                }).OnComplete(this, pool =>
            {
                pool.Return(entry);
            });
        }

        private void Return(FloatingEntry entry)
        {
            if (_disposed || entry.Root == null) return;

            _active.Remove(entry);

            if (entry.CurrentTween.isAlive)
                entry.CurrentTween.Stop();

            entry.Container?.Clear();
            entry.Root.style.display = DisplayStyle.None;
            entry.Root.style.left = 0;
            entry.Root.style.top = 0;
            entry.Root.style.opacity = 1f;

            if (_available.Count >= MaxPoolSize)
            {
                entry.Root.RemoveFromHierarchy();
                return;
            }

            _available.Enqueue(entry);
        }

        private FloatingEntry CreateEntry()
        {
            var root = new VisualElement();
            root.name = $"floating-text_{_instanceCount++}";
            root.style.position = Position.Absolute;
            root.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            root.pickingMode = PickingMode.Ignore;
            root.style.display = DisplayStyle.None;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            container.style.backgroundColor = Color.black;
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 3;
            container.style.paddingBottom = 3;
            container.pickingMode = PickingMode.Ignore;
            root.Add(container);

            _overlayRoot.Add(root);

            return new FloatingEntry { Root = root, Container = container };
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

        private sealed class FloatingEntry
        {
            public VisualElement Root;
            public VisualElement Container;
            public Tween CurrentTween;
        }
    }
}
