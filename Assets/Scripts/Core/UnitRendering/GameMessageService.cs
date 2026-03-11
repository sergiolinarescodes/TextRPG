using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class GameMessageService : IGameMessageService, IDisposable
    {
        private const int MaxPoolSize = 8;
        private const float ArcTravelDuration = 0.6f;
        private const float BounceDuration = 1.0f;
        private const float FadeDuration = 0.5f;
        private const float ArcHeight = 80f;
        private const float LandingRangeX = 60f;
        private const float MessageFontSize = 30f;
        private const float MessageSpacingY = 40f;

        private VisualElement _overlayRoot;
        private readonly Queue<MessageEntry> _available = new();
        private readonly HashSet<MessageEntry> _active = new();
        private readonly System.Random _rng = new();
        private int _instanceCount;
        private bool _disposed;

        public static VisualElement CreateOverlay()
        {
            var overlay = new VisualElement { name = "game-message-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.pickingMode = PickingMode.Ignore;
            return overlay;
        }

        public void Initialize(VisualElement overlayRoot, int prewarmCount = 4)
        {
            _overlayRoot = overlayRoot;
            _instanceCount = 0;

            for (int i = 0; i < prewarmCount; i++)
                _available.Enqueue(CreateEntry());
        }

        public void Spawn(Vector2 sourcePos, string message, Color color)
        {
            if (_disposed || _overlayRoot == null) return;

            MessageEntry entry;
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

            entry.Label.text = message;
            entry.Label.style.color = color;

            // Land at screen center with slight random offset, stacked upward
            float centerX = _overlayRoot.resolvedStyle.width / 2;
            float centerY = _overlayRoot.resolvedStyle.height / 2;
            float landingX = centerX + (float)(_rng.NextDouble() * LandingRangeX * 2 - LandingRangeX);
            float landingY = centerY - _active.Count * MessageSpacingY;

            entry.Root.style.display = DisplayStyle.Flex;
            entry.Root.style.opacity = 1f;

            // Phase 1: Arc travel from source to landing
            entry.ArcTween = Tween.Custom(entry, 0f, 1f, ArcTravelDuration,
                onValueChange: (e, t) =>
                {
                    if (e.Root == null) return;
                    float x = Mathf.Lerp(sourcePos.x, landingX, t);
                    float y = Mathf.Lerp(sourcePos.y, landingY, t);
                    float arc = 4f * ArcHeight * t * (1f - t);
                    y -= arc;
                    e.Root.style.left = x;
                    e.Root.style.top = y;
                },
                ease: Ease.InOutQuad
            ).OnComplete(this, pool =>
            {
                // Phase 2: Bounce at landing
                pool.StartBounce(entry, landingX, landingY);
            });
        }

        private void StartBounce(MessageEntry entry, float landingX, float landingY)
        {
            if (_disposed || entry.Root == null) return;

            float bounceHeight = 40f;
            float startY = landingY - bounceHeight;
            entry.Root.style.left = landingX;
            entry.Root.style.top = startY;

            entry.BounceTween = Tween.Custom(entry, 0f, 1f, BounceDuration,
                onValueChange: (e, t) =>
                {
                    if (e.Root == null) return;
                    float y = Mathf.Lerp(startY, landingY, t);
                    e.Root.style.top = y;
                },
                ease: Ease.OutBounce
            ).OnComplete(this, pool =>
            {
                pool.StartFade(entry);
            });
        }

        private void StartFade(MessageEntry entry)
        {
            if (_disposed || entry.Root == null)
            {
                Return(entry);
                return;
            }

            entry.FadeTween = Tween.Custom(entry, 1f, 0f, FadeDuration,
                onValueChange: (e, t) =>
                {
                    if (e.Root == null) return;
                    e.Root.style.opacity = t;
                }).OnComplete(this, pool =>
            {
                pool.Return(entry);
            });
        }

        private void Return(MessageEntry entry)
        {
            if (_disposed || entry.Root == null) return;

            _active.Remove(entry);
            StopTweens(entry);

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

        private MessageEntry CreateEntry()
        {
            var root = new VisualElement();
            root.name = $"game-msg_{_instanceCount++}";
            root.style.position = Position.Absolute;
            root.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            root.pickingMode = PickingMode.Ignore;
            root.style.display = DisplayStyle.None;

            var label = new Label();
            label.style.fontSize = MessageFontSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.textShadow = new TextShadow
            {
                offset = new Vector2(1, 1),
                blurRadius = 3,
                color = Color.black
            };
            label.pickingMode = PickingMode.Ignore;
            root.Add(label);

            _overlayRoot.Add(root);

            return new MessageEntry { Root = root, Label = label };
        }

        private static void StopTweens(MessageEntry entry)
        {
            if (entry.ArcTween.isAlive) entry.ArcTween.Stop();
            if (entry.BounceTween.isAlive) entry.BounceTween.Stop();
            if (entry.FadeTween.isAlive) entry.FadeTween.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var entry in _active)
            {
                StopTweens(entry);
                entry.Root?.RemoveFromHierarchy();
            }
            _active.Clear();

            while (_available.Count > 0)
            {
                var entry = _available.Dequeue();
                StopTweens(entry);
                entry.Root?.RemoveFromHierarchy();
            }
        }

        private sealed class MessageEntry
        {
            public VisualElement Root;
            public Label Label;
            public Tween ArcTween;
            public Tween BounceTween;
            public Tween FadeTween;
        }
    }
}
