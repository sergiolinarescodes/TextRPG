using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Scroll;
using TextRPG.Core.WordCooldown;
using Unidad.Core.EventBus;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class GameMessageController : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        private Func<EntityId, Vector3> _positionProvider;

        public GameMessageService GameMessages { get; private set; }

        public GameMessageController(IEventBus eventBus, Func<EntityId, Vector3> positionProvider, EntityId playerId)
        {
            _positionProvider = positionProvider;
            GameMessages = new GameMessageService();

            _subscriptions.Add(eventBus.Subscribe<InteractionMessageEvent>(evt =>
            {
                var pos = _positionProvider?.Invoke(evt.SourceEntityId) ?? Vector3.zero;
                GameMessages?.Spawn(new Vector2(pos.x, pos.y), evt.Message, new Color(1f, 0.9f, 0.5f));
            }));
            _subscriptions.Add(eventBus.Subscribe<WordCooldownEvent>(evt =>
            {
                var msg = evt.Permanent
                    ? $"\"{evt.Word}\" is permanently exhausted!"
                    : $"\"{evt.Word}\" on cooldown ({evt.RemainingRounds} rounds)";
                var pos = _positionProvider?.Invoke(playerId) ?? Vector3.zero;
                GameMessages?.Spawn(new Vector2(pos.x, pos.y), msg, new Color(1f, 0.4f, 0.4f));
            }));
            _subscriptions.Add(eventBus.Subscribe<SpellLearnedEvent>(evt =>
            {
                var pos = _positionProvider?.Invoke(playerId) ?? Vector3.zero;
                GameMessages?.Spawn(new Vector2(pos.x, pos.y),
                    $"Learned: {evt.ScrambledWord.ToUpperInvariant()}", ScrollDefinition.ScrollPurple);
            }));
            _subscriptions.Add(eventBus.Subscribe<ScrollAcquiredEvent>(evt =>
            {
                var pos = _positionProvider?.Invoke(playerId) ?? Vector3.zero;
                GameMessages?.Spawn(new Vector2(pos.x, pos.y),
                    $"Scroll: {evt.Scroll.DisplayName}", ScrollDefinition.ScrollPurple);
            }));
        }

        public VisualElement CreateOverlay()
        {
            var overlay = GameMessageService.CreateOverlay();
            GameMessages.Initialize(overlay);
            return overlay;
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            GameMessages?.Dispose();
            GameMessages = null;
            _positionProvider = null;
        }
    }
}
