using System;
using System.Collections.Generic;
using PrimeTween;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Experience;
using Unidad.Core.EventBus;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class ExperienceVisualController : IDisposable
    {
        private const int MaxPoolSize = 4;
        private const float ArcTravelDuration = 0.6f;
        private const float FadeDuration = 0.3f;
        private const float BarFillDuration = 0.4f;
        private const float ArcHeight = 60f;
        private static readonly Color ExpColor = new(1f, 0.85f, 0.2f);

        private readonly IExperienceService _experienceService;
        private readonly PlayerStatsBarVisual _statsBar;
        private readonly Func<EntityId, Vector3> _positionProvider;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly Queue<ExpEntry> _available = new();
        private readonly HashSet<ExpEntry> _active = new();

        private VisualElement _overlayRoot;
        private int _instanceCount;
        private bool _disposed;

        public ExperienceVisualController(
            IEventBus eventBus,
            IExperienceService experienceService,
            PlayerStatsBarVisual statsBar,
            Func<EntityId, Vector3> positionProvider)
        {
            _experienceService = experienceService;
            _statsBar = statsBar;
            _positionProvider = positionProvider;

            _subscriptions.Add(eventBus.Subscribe<ExperienceGainedEvent>(OnExperienceGained));
            _subscriptions.Add(eventBus.Subscribe<LevelUpEvent>(OnLevelUp));
        }

        public VisualElement CreateOverlay()
        {
            _overlayRoot = new VisualElement { name = "exp-overlay" };
            _overlayRoot.style.position = Position.Absolute;
            _overlayRoot.style.left = 0;
            _overlayRoot.style.top = 0;
            _overlayRoot.style.right = 0;
            _overlayRoot.style.bottom = 0;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            for (int i = 0; i < 2; i++)
                _available.Enqueue(CreateEntry());

            return _overlayRoot;
        }

        private void OnExperienceGained(ExperienceGainedEvent evt)
        {
            if (_disposed || _overlayRoot == null) return;

            // Read position live — entity is still in its slot at this point
            var pos3 = _positionProvider?.Invoke(evt.KilledEntity) ?? Vector3.zero;
            var sourcePos = new Vector2(pos3.x, pos3.y);

            var xpBarBounds = _statsBar.XpBar?.worldBound ?? default;
            var destPos = new Vector2(xpBarBounds.center.x, xpBarBounds.center.y);

            ExpEntry entry;
            if (_available.Count > 0)
                entry = _available.Dequeue();
            else if (_active.Count >= MaxPoolSize)
                return;
            else
                entry = CreateEntry();

            _active.Add(entry);
            entry.Label.text = $"+{evt.XpAmount} EXP";
            entry.Label.style.color = ExpColor;
            entry.Root.style.display = DisplayStyle.Flex;
            entry.Root.style.opacity = 1f;

            float newProgress = _experienceService.XpProgress;

            entry.ArcTween = Tween.Custom(entry, 0f, 1f, ArcTravelDuration,
                onValueChange: (e, t) =>
                {
                    if (e.Root == null) return;
                    float x = Mathf.Lerp(sourcePos.x, destPos.x, t);
                    float y = Mathf.Lerp(sourcePos.y, destPos.y, t);
                    float arc = 4f * ArcHeight * t * (1f - t);
                    y -= arc;
                    e.Root.style.left = x;
                    e.Root.style.top = y;
                },
                ease: Ease.InOutQuad
            ).OnComplete(this, ctrl =>
            {
                ctrl.StartFade(entry);

                if (ctrl._statsBar?.XpBar != null)
                {
                    float currentBarValue = ctrl._statsBar.XpBar.Value;
                    Tween.Custom(ctrl._statsBar.XpBar, currentBarValue, Mathf.Clamp01(newProgress),
                        BarFillDuration,
                        onValueChange: (bar, v) => bar.Value = v,
                        ease: Ease.OutQuad);
                }
            });
        }

        private void OnLevelUp(LevelUpEvent evt)
        {
            if (_statsBar == null) return;

            var bar = _statsBar.XpBar;
            if (bar != null)
            {
                float remainder = _experienceService.XpProgress;

                // Animate bar to full
                Tween.Custom(bar, bar.Value, 1f, 0.3f,
                    onValueChange: (b, v) => b.Value = v,
                    ease: Ease.OutQuad
                ).OnComplete(_statsBar, sb =>
                {
                    // Update label, then reset bar to remainder
                    sb.UpdateLevelDisplay(evt.NewLevel, 0f);
                    Tween.Custom(sb.XpBar, 0f, Mathf.Clamp01(remainder), 0.3f,
                        onValueChange: (b, v) => b.Value = v,
                        ease: Ease.OutQuad);
                });
            }
            else
            {
                _statsBar.UpdateLevelDisplay(evt.NewLevel, _experienceService.XpProgress);
            }
        }

        private void StartFade(ExpEntry entry)
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
                }).OnComplete(this, ctrl => ctrl.Return(entry));
        }

        private void Return(ExpEntry entry)
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

        private ExpEntry CreateEntry()
        {
            var root = new VisualElement();
            root.name = $"exp-entry_{_instanceCount++}";
            root.style.position = Position.Absolute;
            root.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            root.pickingMode = PickingMode.Ignore;
            root.style.display = DisplayStyle.None;

            var label = new Label();
            label.style.fontSize = 28;
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

            _overlayRoot?.Add(root);
            return new ExpEntry { Root = root, Label = label };
        }

        private static void StopTweens(ExpEntry entry)
        {
            if (entry.ArcTween.isAlive) entry.ArcTween.Stop();
            if (entry.FadeTween.isAlive) entry.FadeTween.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

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

        private sealed class ExpEntry
        {
            public VisualElement Root;
            public Label Label;
            public Tween ArcTween;
            public Tween FadeTween;
        }
    }
}
