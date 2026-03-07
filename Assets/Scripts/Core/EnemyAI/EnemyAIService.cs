using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Patterns.Scoring;
using Unidad.Core.Systems;

namespace TextRPG.Core.EnemyAI
{
    internal sealed class EnemyAIService : SystemServiceBase, IEnemyAIService
    {
        private readonly IEncounterService _encounterService;
        private readonly IEntityStatsService _entityStats;
        private readonly ITurnService _turnService;
        private readonly ICombatGridService _combatGrid;
        private readonly ICombatContext _combatContext;
        private readonly IActionExecutionService _actionExecution;
        private readonly IContributorRegistry<AIDecisionContext> _scorers;
        private readonly EnemyWordResolver _enemyResolver;

        public EnemyAIService(IEventBus eventBus, IEncounterService encounterService,
            IEntityStatsService entityStats, ITurnService turnService, ICombatGridService combatGrid,
            ICombatContext combatContext, IActionExecutionService actionExecution,
            IContributorRegistry<AIDecisionContext> scorers, EnemyWordResolver enemyResolver)
            : base(eventBus)
        {
            _encounterService = encounterService;
            _entityStats = entityStats;
            _turnService = turnService;
            _combatGrid = combatGrid;
            _combatContext = combatContext;
            _actionExecution = actionExecution;
            _scorers = scorers;
            _enemyResolver = enemyResolver;

            Subscribe<TurnStartedEvent>(OnTurnStarted);
        }

        public void ProcessTurn(EntityId enemyId)
        {
            if (!_encounterService.IsEnemy(enemyId))
                return;

            if (!_turnService.IsTurnActive)
                return;

            var def = _encounterService.GetEnemyDefinition(enemyId);
            var movementPoints = _entityStats.GetStat(enemyId, StatType.MovementPoints);

            MoveTowardPlayer(enemyId, movementPoints, def.Abilities);

            var bestAbility = PickBestAbility(enemyId, def.Abilities);
            if (bestAbility == null)
                return;

            var savedSource = _combatContext.SourceEntity;
            var savedEnemies = new List<EntityId>();

            _combatContext.SetSourceEntity(enemyId);
            _combatContext.SetEnemies(new[] { savedSource });

            _actionExecution.ExecuteWord(bestAbility);

            _combatContext.SetSourceEntity(savedSource);
            _combatContext.SetEnemies(_encounterService.EnemyEntities);
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            if (!_encounterService.IsEncounterActive || !_encounterService.IsEnemy(e.EntityId))
                return;

            ProcessTurn(e.EntityId);
        }

        private void MoveTowardPlayer(EntityId enemyId, int movementPoints, string[] abilities)
        {
            var hasMelee = false;
            foreach (var ability in abilities)
            {
                if (_enemyResolver.HasWord(ability))
                {
                    var meta = _enemyResolver.GetStats(ability);
                    if (meta.Target == "Melee")
                    {
                        hasMelee = true;
                        break;
                    }
                }
            }

            if (!hasMelee || movementPoints <= 0)
                return;

            var enemyPos = _combatGrid.GetPosition(enemyId);
            var adjacent = _combatGrid.GetAdjacentEntities(enemyPos);
            if (adjacent.Count > 0)
                return;

            for (int step = 0; step < movementPoints; step++)
            {
                var currentPos = _combatGrid.GetPosition(enemyId);
                var bestPos = currentPos;
                var bestDist = int.MaxValue;

                foreach (var neighbor in _combatGrid.Grid.GetNeighbors(currentPos, NeighborMode.Cardinal))
                {
                    if (!_combatGrid.CanMoveTo(neighbor))
                        continue;

                    var entity = FindNearestNonEnemy(neighbor);
                    if (entity == null) continue;

                    var dist = neighbor.ManhattanDistanceTo(_combatGrid.GetPosition(entity.Value));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPos = neighbor;
                    }
                }

                if (bestPos.Equals(currentPos))
                    break;

                _combatGrid.MoveEntity(enemyId, bestPos);

                if (_combatGrid.GetAdjacentEntities(bestPos).Count > 0)
                    break;
            }
        }

        private EntityId? FindNearestNonEnemy(GridPosition from)
        {
            EntityId? nearest = null;
            var bestDist = int.MaxValue;

            foreach (var pos in _combatGrid.Grid.AllPositions)
            {
                var entity = _combatGrid.GetEntityAt(pos);
                if (entity == null || _encounterService.IsEnemy(entity.Value))
                    continue;

                var dist = from.ManhattanDistanceTo(pos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = entity;
                }
            }

            return nearest;
        }

        private string PickBestAbility(EntityId enemyId, string[] abilities)
        {
            string best = null;
            float bestScore = float.MinValue;

            var enemyHp = _entityStats.GetCurrentHealth(enemyId);
            var enemyMaxHp = _entityStats.GetStat(enemyId, StatType.MaxHealth);
            var enemyPos = _combatGrid.GetPosition(enemyId);

            foreach (var ability in abilities)
            {
                if (!_enemyResolver.HasWord(ability))
                    continue;

                var meta = _enemyResolver.GetStats(ability);
                var isMelee = meta.Target == "Melee";

                var targetId = FindNearestNonEnemy(enemyPos) ?? default;
                if (targetId.Equals(default(EntityId)))
                    continue;

                var targetHp = _entityStats.GetCurrentHealth(targetId);
                var targetMaxHp = _entityStats.GetStat(targetId, StatType.MaxHealth);
                var distance = enemyPos.ManhattanDistanceTo(_combatGrid.GetPosition(targetId));

                var ctx = new AIDecisionContext(enemyId, targetId, ability,
                    enemyHp, enemyMaxHp, targetHp, targetMaxHp, distance, isMelee);

                var score = _scorers.EvaluateAll(ctx);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ability;
                }
            }

            return best;
        }

    }
}
