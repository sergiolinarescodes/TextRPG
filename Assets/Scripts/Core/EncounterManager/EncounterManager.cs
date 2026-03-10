using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EncounterManager
{
    internal sealed class EncounterManager : SystemServiceBase, IEncounterManager, ICombatModeService
    {
        private readonly IEncounterService _combatEncounterService;
        private readonly IEventEncounterService _eventEncounterService;
        private readonly EventEncounterProviderRegistry _providerRegistry;

        private readonly Stack<EventEncounterDefinition> _eventStack = new();
        private EventEncounterDefinition _returnPoint;
        private EntityId _player;
        private bool _isInCombat;

        public bool IsInCombat => _combatEncounterService.IsEncounterActive;
        public bool IsInEvent => _eventEncounterService.IsEncounterActive;
        bool ICombatModeService.IsInCombat => _isInCombat;

        public EncounterManager(
            IEventBus eventBus,
            IEncounterService combatEncounterService,
            IEventEncounterService eventEncounterService,
            EventEncounterProviderRegistry providerRegistry) : base(eventBus)
        {
            _combatEncounterService = combatEncounterService;
            _eventEncounterService = eventEncounterService;
            _providerRegistry = providerRegistry;

            Subscribe<EventEncounterTransitionEvent>(OnTransitionEvent);
            Subscribe<SpawnCombatEvent>(OnSpawnCombat);
            Subscribe<EncounterEndedEvent>(OnCombatEnded);
        }

        public void SetCombatMode(bool inCombat)
        {
            if (_isInCombat == inCombat) return;
            _isInCombat = inCombat;
            Publish(new CombatModeChangedEvent(inCombat));
        }

        public void StartCombatEncounter(EncounterDefinition encounter, EntityId player)
        {
            _player = player;
            SetCombatMode(true);
            _combatEncounterService.StartEncounter(encounter, player);
        }

        public void StartEventEncounter(EventEncounterDefinition encounter, EntityId player)
        {
            _player = player;
            _eventStack.Clear();
            _returnPoint = null;
            SetCombatMode(false);
            _eventEncounterService.StartEncounter(encounter, player);
            _eventStack.Push(encounter);
        }

        public void TransitionToEvent(EventEncounterDefinition encounter)
        {
            if (_eventEncounterService.IsEncounterActive)
                _eventEncounterService.EndEncounter();

            SetCombatMode(false);
            _eventEncounterService.StartEncounter(encounter, _player);
            _eventStack.Push(encounter);
        }

        public void TransitionToCombat(EncounterDefinition encounter)
        {
            if (_eventEncounterService.IsEncounterActive)
            {
                _returnPoint = _eventStack.Count > 0 ? _eventStack.Peek() : null;
                _eventEncounterService.EndEncounter();
            }

            SetCombatMode(true);
            _combatEncounterService.StartEncounter(encounter, _player);
        }

        public void ReturnToPrevious()
        {
            if (_eventStack.Count <= 1)
            {
                if (_eventEncounterService.IsEncounterActive)
                    _eventEncounterService.EndEncounter();
                _eventStack.Clear();
                return;
            }

            if (_eventEncounterService.IsEncounterActive)
                _eventEncounterService.EndEncounter();

            _eventStack.Pop();
            var previous = _eventStack.Peek();
            SetCombatMode(false);
            _eventEncounterService.StartEncounter(previous, _player);
        }

        private void OnTransitionEvent(EventEncounterTransitionEvent evt)
        {
            if (!_providerRegistry.TryGet(evt.TargetEncounterId, out var provider)) return;
            TransitionToEvent(provider.CreateDefinition());
        }

        private void OnSpawnCombat(SpawnCombatEvent evt)
        {
            Debug.Log($"[EncounterManager] SpawnCombat requested: {evt.EncounterId}");
            // TODO: Resolve combat encounter from a combat encounter registry when available
        }

        private void OnCombatEnded(EncounterEndedEvent evt)
        {
            if (_returnPoint == null) return;

            var ret = _returnPoint;
            _returnPoint = null;
            SetCombatMode(false);
            _eventEncounterService.StartEncounter(ret, _player);
        }
    }
}
