using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter
{
    internal sealed class EventEncounterService : SystemServiceBase, IEventEncounterService
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ICombatSlotService _slotService;
        private readonly ICombatContext _combatContext;
        private readonly ReactionService _reactionService;

        private readonly List<EntityId> _interactableEntities = new();
        private readonly Dictionary<EntityId, InteractableDefinition> _definitions = new();
        private readonly Dictionary<EntityId, EntityDefinition> _entityDefinitions = new();
        private string _activeEncounterId;
        private EntityId _player;

        public bool IsEncounterActive => _activeEncounterId != null;
        public IReadOnlyList<EntityId> InteractableEntities => _interactableEntities;
        public EntityId PlayerEntity => _player;

        public EventEncounterService(
            IEventBus eventBus,
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            ICombatContext combatContext,
            ReactionService reactionService) : base(eventBus)
        {
            _entityStats = entityStats;
            _slotService = slotService;
            _combatContext = combatContext;
            _reactionService = reactionService;

            Subscribe<EntityDiedEvent>(OnEntityDied);
            Subscribe<EntityRecruitedEvent>(OnEntityRecruited);
        }

        public void StartEncounter(EventEncounterDefinition encounter, EntityId player)
        {
            if (IsEncounterActive)
                throw new InvalidOperationException("An event encounter is already active.");

            _activeEncounterId = encounter.Id;
            _player = player;
            _interactableEntities.Clear();
            _definitions.Clear();
            _entityDefinitions.Clear();

            _slotService.Initialize();

            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");

                _entityStats.RegisterEntity(entityId, def.MaxHealth, 0, 0, 0, 0, 0);
                _slotService.RegisterEnemy(entityId, i);

                _interactableEntities.Add(entityId);
                _definitions[entityId] = def;

                // Build unified EntityDefinition from interactable data
                _entityDefinitions[entityId] = new EntityDefinition(
                    def.Name, def.MaxHealth, 0, 0, 0, 0, 0, def.Color,
                    Array.Empty<string>(), 0, "interactable", def.Passives,
                    def.Tags, def.Description, def.DeathReward, def.DeathRewardValue);

                _reactionService.RegisterReactions(entityId, def.Reactions, def.Tags);
            }

            _combatContext.SetSourceEntity(player);
            _combatContext.SetEnemies(_interactableEntities);
            _combatContext.SetAllies(Array.Empty<EntityId>());
            _combatContext.SetSlotService(_slotService);

            Publish(new EventEncounterStartedEvent(encounter.Id, encounter.Interactables.Length));
        }

        public void EndEncounter()
        {
            if (!IsEncounterActive) return;

            var id = _activeEncounterId;
            _activeEncounterId = null;

            foreach (var entityId in _interactableEntities)
            {
                if (_entityStats.HasEntity(entityId))
                    _entityStats.RemoveEntity(entityId);
            }

            _interactableEntities.Clear();
            _definitions.Clear();
            _entityDefinitions.Clear();
            _reactionService.ClearReactions();

            Publish(new EventEncounterEndedEvent(id));
        }

        public InteractableDefinition GetDefinition(EntityId entityId)
        {
            if (!_definitions.TryGetValue(entityId, out var def))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not an interactable.");
            return def;
        }

        public EntityDefinition GetEntityDefinition(EntityId entityId)
        {
            if (!_entityDefinitions.TryGetValue(entityId, out var def))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not an interactable.");
            return def;
        }

        private void OnEntityRecruited(EntityRecruitedEvent e)
        {
            if (!IsEncounterActive) return;

            // Remove from interactable tracking (also updates CombatContext enemies since it holds same list reference)
            _interactableEntities.Remove(e.EntityId);
            _definitions.Remove(e.EntityId);
            _entityDefinitions.Remove(e.EntityId);
            _reactionService.ClearReactions(e.EntityId);
        }

        private void OnEntityDied(EntityDiedEvent e)
        {
            if (!IsEncounterActive || !_definitions.TryGetValue(e.EntityId, out var def))
                return;

            if (def.DeathReward != null)
                Publish(new RewardGrantedEvent(def.DeathReward, def.DeathRewardValue));

            _interactableEntities.Remove(e.EntityId);
            _definitions.Remove(e.EntityId);
            _entityDefinitions.Remove(e.EntityId);
            _slotService.RemoveEntity(e.EntityId);
            _reactionService.ClearReactions(e.EntityId);
        }
    }
}
