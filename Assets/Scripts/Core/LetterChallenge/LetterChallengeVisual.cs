using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.LetterChallenge
{
    internal sealed class LetterChallengeVisual : IDisposable
    {
        private static readonly Color LetterColor = new(0.6f, 0.75f, 0.9f);
        private static readonly Color MatchColor = new(0.3f, 1f, 0.4f);
        private static readonly Color BorderColor = new(0.4f, 0.55f, 0.75f);

        private readonly List<IDisposable> _subscriptions = new();
        private readonly VisualElement _container;
        private readonly Label _letterLabel;
        private readonly EntityId _playerId;
        private IVisualElementScheduledItem _danceSchedule;
        private float _danceTime;
        private bool _disposed;

        public LetterChallengeVisual(IEventBus eventBus, VisualElement parent, EntityId playerId)
        {
            _playerId = playerId;

            _container = new VisualElement();
            _container.style.position = Position.Absolute;
            _container.style.right = 8;
            _container.style.bottom = 8;
            _container.style.width = 52;
            _container.style.height = 52;
            _container.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
            _container.style.borderTopWidth = 2;
            _container.style.borderBottomWidth = 2;
            _container.style.borderLeftWidth = 2;
            _container.style.borderRightWidth = 2;
            _container.style.borderTopColor = BorderColor;
            _container.style.borderBottomColor = BorderColor;
            _container.style.borderLeftColor = BorderColor;
            _container.style.borderRightColor = BorderColor;
            _container.style.borderTopLeftRadius = 6;
            _container.style.borderTopRightRadius = 6;
            _container.style.borderBottomLeftRadius = 6;
            _container.style.borderBottomRightRadius = 6;
            _container.style.justifyContent = Justify.Center;
            _container.style.alignItems = Align.Center;
            _container.style.display = DisplayStyle.None;

            _letterLabel = new Label();
            _letterLabel.style.color = LetterColor;
            _letterLabel.style.fontSize = 28;
            _letterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _letterLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _letterLabel.pickingMode = PickingMode.Ignore;
            _container.Add(_letterLabel);

            parent.Add(_container);

            _subscriptions.Add(eventBus.Subscribe<LetterChallengeStartedEvent>(OnChallengeStarted));
            _subscriptions.Add(eventBus.Subscribe<LetterChallengeMatchedEvent>(OnChallengeMatched));
            _subscriptions.Add(eventBus.Subscribe<LetterChallengeClearedEvent>(OnChallengeCleared));

            StartDanceAnimation();
        }

        private void OnChallengeStarted(LetterChallengeStartedEvent evt)
        {
            if (!evt.Owner.Equals(_playerId)) return;

            _letterLabel.text = evt.Letters.ToUpperInvariant();
            _letterLabel.style.color = LetterColor;
            _container.style.display = DisplayStyle.Flex;

            // Jump-scale animation: scale up then settle
            _container.style.scale = new Scale(new Vector3(1.6f, 1.6f, 1f));
            _container.schedule.Execute(() =>
            {
                if (_disposed) return;
                _container.style.scale = new Scale(new Vector3(1f, 1f, 1f));
            }).ExecuteLater(120);
        }

        private void OnChallengeMatched(LetterChallengeMatchedEvent evt)
        {
            if (!evt.Owner.Equals(_playerId)) return;

            // Green flash on match
            _letterLabel.style.color = MatchColor;
            _container.style.scale = new Scale(new Vector3(1.3f, 1.3f, 1f));

            _container.schedule.Execute(() =>
            {
                if (_disposed) return;
                _letterLabel.style.color = LetterColor;
                _container.style.scale = new Scale(new Vector3(1f, 1f, 1f));
            }).ExecuteLater(200);
        }

        private void OnChallengeCleared(LetterChallengeClearedEvent evt)
        {
            if (!evt.Owner.Equals(_playerId)) return;

            _container.style.display = DisplayStyle.None;
            _letterLabel.text = "";
        }

        private void StartDanceAnimation()
        {
            _danceTime = 0f;
            _danceSchedule = _container.schedule.Execute(() =>
            {
                if (_disposed) return;
                _danceTime += 0.05f;

                // Gentle wobble: slight Y oscillation + rotation
                var offsetY = Mathf.Sin(_danceTime * 3f) * 2f;
                var rotation = Mathf.Sin(_danceTime * 2.2f) * 2.5f;

                _letterLabel.style.translate = new Translate(0, offsetY);
                _letterLabel.style.rotate = new Rotate(Angle.Degrees(rotation));
            }).Every(50);
        }

        public void Dispose()
        {
            _disposed = true;
            _danceSchedule?.Pause();
            _danceSchedule = null;

            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _container?.RemoveFromHierarchy();
        }
    }
}
