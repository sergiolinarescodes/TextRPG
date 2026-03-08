using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.TurnSystem
{
    internal sealed class TurnService : SystemServiceBase, ITurnService
    {
        private readonly List<EntityId> _turnOrder = new();
        private readonly Queue<EntityId> _extraTurns = new();
        private int _currentIndex;

        public EntityId CurrentEntity => _turnOrder.Count > 0 ? _turnOrder[_currentIndex] : default;
        public int CurrentTurnNumber { get; private set; }
        public int CurrentRoundNumber { get; private set; } = 1;
        public bool IsTurnActive { get; private set; }

        public TurnService(IEventBus eventBus) : base(eventBus) { }

        public void SetTurnOrder(IReadOnlyList<EntityId> order)
        {
            if (order == null || order.Count == 0)
                throw new ArgumentException("Turn order must contain at least one entity.", nameof(order));

            _turnOrder.Clear();
            _turnOrder.AddRange(order);
            _extraTurns.Clear();
            _currentIndex = 0;
            CurrentTurnNumber = 0;
            CurrentRoundNumber = 1;
            IsTurnActive = false;
        }

        public void AddToTurnOrder(EntityId entityId)
        {
            if (!_turnOrder.Contains(entityId))
                _turnOrder.Add(entityId);
        }

        public void BeginTurn()
        {
            if (_turnOrder.Count == 0)
                throw new InvalidOperationException("Turn order has not been set.");
            if (IsTurnActive)
                throw new InvalidOperationException("A turn is already active.");

            IsTurnActive = true;
            CurrentTurnNumber++;
            Publish(new TurnStartedEvent(CurrentEntity, CurrentTurnNumber));
        }

        public void EndTurn()
        {
            if (!IsTurnActive)
                throw new InvalidOperationException("No turn is currently active.");

            var entity = CurrentEntity;
            var turnNumber = CurrentTurnNumber;
            IsTurnActive = false;
            Publish(new TurnEndedEvent(entity, turnNumber));

            if (_extraTurns.Count > 0)
            {
                var extraEntity = _extraTurns.Dequeue();
                var idx = _turnOrder.IndexOf(extraEntity);
                if (idx >= 0)
                    _currentIndex = idx;
            }
            else
            {
                _currentIndex++;
                if (_currentIndex >= _turnOrder.Count)
                {
                    Publish(new RoundEndedEvent(CurrentRoundNumber));
                    _currentIndex = 0;
                    CurrentRoundNumber++;
                    Publish(new RoundStartedEvent(CurrentRoundNumber));
                }
            }
        }

        public void RemoveFromTurnOrder(EntityId entityId)
        {
            var idx = _turnOrder.IndexOf(entityId);
            if (idx < 0) return;
            _turnOrder.RemoveAt(idx);
            if (_turnOrder.Count == 0) return;
            if (idx < _currentIndex)
                _currentIndex--;
            else if (idx == _currentIndex && _currentIndex >= _turnOrder.Count)
                _currentIndex = 0;
        }

        public void GrantExtraTurn(EntityId entityId)
        {
            _extraTurns.Enqueue(entityId);
            Publish(new ExtraTurnGrantedEvent(entityId));
        }
    }
}
