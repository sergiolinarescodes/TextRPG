using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Encounter
{
    internal sealed class EncounterService : SystemServiceBase, IEncounterService
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ITurnService _turnService;
        private readonly ICombatSlotService _slotService;
        private readonly ICombatContext _combatContext;
        private readonly EnemyWordResolver _enemyWordResolver;

        private readonly List<EntityId> _enemyEntities = new();
        private readonly Dictionary<EntityId, EnemyDefinition> _enemyDefinitions = new();
        private string _activeEncounterId;
        private EntityId _player;

        public bool IsEncounterActive => _activeEncounterId != null;
        public IReadOnlyList<EntityId> EnemyEntities => _enemyEntities;
        public EntityId PlayerEntity => _player;
        public EnemyWordResolver EnemyResolver => _enemyWordResolver;

        public EncounterService(IEventBus eventBus, IEntityStatsService entityStats, ITurnService turnService,
            ICombatSlotService slotService, ICombatContext combatContext, EnemyWordResolver enemyWordResolver)
            : base(eventBus)
        {
            _entityStats = entityStats;
            _turnService = turnService;
            _slotService = slotService;
            _combatContext = combatContext;
            _enemyWordResolver = enemyWordResolver;
            Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        public void StartEncounter(EncounterDefinition encounter, EntityId player)
        {
            if (IsEncounterActive)
                throw new InvalidOperationException("An encounter is already active.");

            _activeEncounterId = encounter.Id;
            _player = player;
            _enemyEntities.Clear();
            _enemyDefinitions.Clear();
            _enemyWordResolver.Clear();

            _slotService.Initialize();

            for (int i = 0; i < encounter.Enemies.Length; i++)
            {
                var enemyDef = encounter.Enemies[i];
                var entityId = new EntityId($"enemy_{enemyDef.Name.ToLowerInvariant()}_{i}");

                _entityStats.RegisterEntity(entityId, enemyDef.MaxHealth, enemyDef.Strength, enemyDef.MagicPower,
                    enemyDef.PhysicalDefense, enemyDef.MagicDefense, enemyDef.Luck,
                    startingShield: enemyDef.StartingShield);

                _slotService.RegisterEnemy(entityId, i);

                _enemyEntities.Add(entityId);
                _enemyDefinitions[entityId] = enemyDef;

                Publish(new EnemySpawnedEvent(entityId, enemyDef.Name));
            }

            var turnOrder = new List<EntityId> { player };
            turnOrder.AddRange(_enemyEntities);
            _turnService.SetTurnOrder(turnOrder);

            _combatContext.SetSourceEntity(player);
            _combatContext.SetEnemies(_enemyEntities);
            _combatContext.SetAllies(Array.Empty<EntityId>());
            _combatContext.SetSlotService(_slotService);

            Publish(new EncounterStartedEvent(encounter.Id, encounter.Enemies.Length));
        }

        public void EndEncounter()
        {
            if (!IsEncounterActive) return;

            var id = _activeEncounterId;
            var victory = true;
            foreach (var enemy in _enemyEntities)
            {
                if (_entityStats.HasEntity(enemy) && _entityStats.GetCurrentHealth(enemy) > 0)
                {
                    victory = false;
                    break;
                }
            }

            _activeEncounterId = null;
            _enemyEntities.Clear();
            _enemyDefinitions.Clear();
            _enemyWordResolver.Clear();
            Publish(new EncounterEndedEvent(id, victory));
        }

        public void RegisterEnemy(EntityId entityId, EnemyDefinition definition = null)
        {
            if (_enemyDefinitions.ContainsKey(entityId))
                return;

            definition ??= new EnemyDefinition("SUMMON", 10, 1, 0, 0, 0, 0, Color.red, new[] { "scratch" });
            _enemyEntities.Add(entityId);
            _enemyDefinitions[entityId] = definition;
        }

        public bool IsEnemy(EntityId entityId) => _enemyDefinitions.ContainsKey(entityId);

        public EnemyDefinition GetEnemyDefinition(EntityId entityId)
        {
            if (!_enemyDefinitions.TryGetValue(entityId, out var def))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not an enemy.");
            return def;
        }

        private void OnEntityDied(EntityDiedEvent e)
        {
            if (!IsEncounterActive || !IsEnemy(e.EntityId))
                return;

            _slotService.RemoveEntity(e.EntityId);
            _turnService.RemoveFromTurnOrder(e.EntityId);

            var allDead = true;
            foreach (var enemy in _enemyEntities)
            {
                if (_entityStats.HasEntity(enemy) && _entityStats.GetCurrentHealth(enemy) > 0)
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead)
                EndEncounter();
        }
    }
}
