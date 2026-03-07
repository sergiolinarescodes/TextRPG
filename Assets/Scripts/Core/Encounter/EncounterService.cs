using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Encounter
{
    internal sealed class EncounterService : SystemServiceBase, IEncounterService
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ITurnService _turnService;
        private readonly ICombatGridService _combatGrid;
        private readonly ICombatContext _combatContext;
        private readonly EnemyWordResolver _enemyWordResolver;

        private readonly List<EntityId> _enemyEntities = new();
        private readonly Dictionary<EntityId, EnemyDefinition> _enemyDefinitions = new();
        private string _activeEncounterId;
        private EntityId _player;

        public bool IsEncounterActive => _activeEncounterId != null;
        public IReadOnlyList<EntityId> EnemyEntities => _enemyEntities;
        public EnemyWordResolver EnemyResolver => _enemyWordResolver;

        public EncounterService(IEventBus eventBus, IEntityStatsService entityStats, ITurnService turnService,
            ICombatGridService combatGrid, ICombatContext combatContext, EnemyWordResolver enemyWordResolver)
            : base(eventBus)
        {
            _entityStats = entityStats;
            _turnService = turnService;
            _combatGrid = combatGrid;
            _combatContext = combatContext;
            _enemyWordResolver = enemyWordResolver;
            Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        public void StartEncounter(EncounterDefinition encounter, EntityId player, GridPosition playerPosition)
        {
            if (IsEncounterActive)
                throw new InvalidOperationException("An encounter is already active.");

            _activeEncounterId = encounter.Id;
            _player = player;
            _enemyEntities.Clear();
            _enemyDefinitions.Clear();
            _enemyWordResolver.Clear();

            _combatGrid.Initialize(8, 6);
            var playerUnitDef = new UnitDefinition(new UnitId(player.Value), "PLAYER", 100, 10, 5, 8, Color.cyan);
            _combatGrid.RegisterCombatant(player, playerUnitDef, playerPosition);

            for (int i = 0; i < encounter.Enemies.Length; i++)
            {
                var enemyDef = encounter.Enemies[i];
                var position = encounter.EnemyPositions[i];
                var entityId = new EntityId($"enemy_{enemyDef.Name.ToLowerInvariant()}_{i}");

                _entityStats.RegisterEntity(entityId, enemyDef.MaxHealth, enemyDef.Strength, enemyDef.MagicPower,
                    enemyDef.PhysicalDefense, enemyDef.MagicDefense, enemyDef.Luck, enemyDef.MovementPoints);

                var unitDef = new UnitDefinition(new UnitId(entityId.Value), enemyDef.Name, enemyDef.MaxHealth,
                    enemyDef.Strength, 0, 0, enemyDef.Color);
                _combatGrid.RegisterCombatant(entityId, unitDef, position);

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
            _combatContext.SetGrid(_combatGrid);

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
