using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Scoring;
using Unidad.Core.Systems;

namespace TextRPG.Core.CombatAI
{
    internal sealed class CombatAIService : SystemServiceBase, ICombatAIService
    {
        private readonly IEncounterService _encounterService;
        private readonly IEntityStatsService _entityStats;
        private readonly ITurnService _turnService;
        private readonly ICombatSlotService _slotService;
        private readonly ICombatContext _combatContext;
        private readonly IActionExecutionService _actionExecution;
        private readonly IContributorRegistry<AIDecisionContext> _scorers;
        private readonly EnemyWordResolver _enemyResolver;
        private readonly IReadOnlyDictionary<string, EnemyDefinition> _unitRegistry;
        private readonly IPassiveService _passiveService;

        private readonly Dictionary<EntityId, string[]> _summonAbilities = new();
        private readonly Dictionary<EntityId, EntityId> _summonOwners = new();

        public CombatAIService(IEventBus eventBus, IEncounterService encounterService,
            IEntityStatsService entityStats, ITurnService turnService, ICombatSlotService slotService,
            ICombatContext combatContext, IActionExecutionService actionExecution,
            IContributorRegistry<AIDecisionContext> scorers, EnemyWordResolver enemyResolver,
            IReadOnlyDictionary<string, EnemyDefinition> unitRegistry = null,
            IPassiveService passiveService = null)
            : base(eventBus)
        {
            _encounterService = encounterService;
            _entityStats = entityStats;
            _turnService = turnService;
            _slotService = slotService;
            _combatContext = combatContext;
            _actionExecution = actionExecution;
            _scorers = scorers;
            _enemyResolver = enemyResolver;
            _unitRegistry = unitRegistry;
            _passiveService = passiveService;

            if (!_enemyResolver.HasWord("scratch"))
                _enemyResolver.RegisterWord("scratch",
                    new List<WordActionMapping> { new("Damage", 1) },
                    new WordMeta("SingleEnemy", 0, 1));

            Subscribe<TurnStartedEvent>(OnTurnStarted);
            Subscribe<UnitSummonedEvent>(OnUnitSummoned);
            Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        public void ProcessTurn(EntityId entityId)
        {
            if (!IsAIControlled(entityId))
                return;

            if (!_turnService.IsTurnActive)
                return;

            string[] abilities;
            if (_encounterService.IsEnemy(entityId))
            {
                var def = _encounterService.GetEnemyDefinition(entityId);
                abilities = def.Abilities;
            }
            else
            {
                abilities = _summonAbilities.TryGetValue(entityId, out var a)
                    ? a : new[] { "scratch" };
            }

            var friendly = IsFriendly(entityId);

            var bestAbility = PickBestAbility(entityId, abilities, friendly);
            if (bestAbility == null)
            {
                Publish(new ActionAnimationCompletedEvent());
                return;
            }

            var savedSource = _combatContext.SourceEntity;
            var savedEnemies = BuildOpponents(entityId, friendly);

            _combatContext.SetSourceEntity(entityId);
            _combatContext.SetEnemies(savedEnemies);
            _combatContext.SetAllies(Array.Empty<EntityId>());

            try
            {
                _actionExecution.ExecuteWord(bestAbility);
            }
            finally
            {
                _combatContext.SetSourceEntity(_encounterService.PlayerEntity);
                _combatContext.SetEnemies(_encounterService.EnemyEntities);
                _combatContext.SetAllies(Array.Empty<EntityId>());
            }
        }

        private bool IsAIControlled(EntityId entityId)
        {
            return _encounterService.IsEnemy(entityId) || _summonOwners.ContainsKey(entityId);
        }

        private bool IsFriendly(EntityId entityId)
        {
            if (_summonOwners.TryGetValue(entityId, out var owner))
                return !_encounterService.IsEnemy(owner);
            return !_encounterService.IsEnemy(entityId);
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            if (!_encounterService.IsEncounterActive)
                return;

            if (!IsAIControlled(e.EntityId))
                return;

            ProcessTurn(e.EntityId);
        }

        private void OnUnitSummoned(UnitSummonedEvent e)
        {
            _summonOwners[e.EntityId] = e.Owner;
            _turnService.AddToTurnOrder(e.EntityId);

            var isEnemySummon = _encounterService.IsEnemy(e.Owner);

            // Use the unit's actual abilities from the registry
            string[] abilities = new[] { "scratch" };
            if (e.Word.Length > 0 && _unitRegistry != null)
            {
                var word = e.Word.ToLowerInvariant();
                if (_unitRegistry.TryGetValue(word, out var def))
                {
                    abilities = def.Abilities; // may be empty for structures
                    if (abilities.Length > 0)
                        UnitDatabaseLoader.RegisterUnitWords(_enemyResolver, word);
                }
            }
            _summonAbilities[e.EntityId] = abilities;

            if (isEnemySummon)
                _encounterService.RegisterEnemy(e.EntityId);
        }

        private void OnEntityDied(EntityDiedEvent e)
        {
            _summonOwners.Remove(e.EntityId);
            _summonAbilities.Remove(e.EntityId);
        }

        private IReadOnlyList<EntityId> BuildOpponents(EntityId entityId, bool friendly)
        {
            if (friendly)
                return _encounterService.EnemyEntities;

            var opponents = new List<EntityId> { _encounterService.PlayerEntity };
            foreach (var kv in _summonOwners)
            {
                if (!_encounterService.IsEnemy(kv.Value))
                    opponents.Add(kv.Key);
            }
            return opponents;
        }

        private string PickBestAbility(EntityId entityId, string[] abilities, bool friendly)
        {
            string best = null;
            float bestScore = 0f;

            var hp = _entityStats.GetCurrentHealth(entityId);
            var maxHp = _entityStats.GetStat(entityId, StatType.MaxHealth);

            foreach (var ability in abilities)
            {
                if (!_enemyResolver.HasWord(ability))
                    continue;

                var meta = _enemyResolver.GetStats(ability);
                var isMelee = meta.Target == "Melee" || meta.Target == "FrontEnemy";

                var targetId = FindFirstTarget(entityId, friendly);
                if (targetId.Equals(default(EntityId)))
                    continue;

                var targetHp = _entityStats.GetCurrentHealth(targetId);
                var targetMaxHp = _entityStats.GetStat(targetId, StatType.MaxHealth);

                // Slot distance: 1 for cross-faction (always), slot diff for same-faction
                int distance = 1;

                var ctx = new AIDecisionContext(entityId, targetId, ability,
                    hp, maxHp, targetHp, targetMaxHp, distance, isMelee);

                var score = _scorers.EvaluateAll(ctx);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ability;
                }
            }

            return best;
        }

        private EntityId FindFirstTarget(EntityId entityId, bool friendly)
        {
            if (friendly)
            {
                // Friendly summon targets enemies — check taunt first
                var enemies = _encounterService.EnemyEntities;
                var taunt = FindTauntEntity(enemies);
                if (!taunt.Equals(default(EntityId))) return taunt;

                for (int i = 0; i < enemies.Count; i++)
                {
                    if (_entityStats.HasEntity(enemies[i]) && _entityStats.GetCurrentHealth(enemies[i]) > 0)
                        return enemies[i];
                }
            }
            else
            {
                // Enemy targets player or player summons — check taunt first
                var opponents = BuildOpponents(entityId, friendly);
                var taunt = FindTauntEntity(opponents);
                if (!taunt.Equals(default(EntityId))) return taunt;

                var player = _encounterService.PlayerEntity;
                if (_entityStats.HasEntity(player) && _entityStats.GetCurrentHealth(player) > 0)
                    return player;
                foreach (var kv in _summonOwners)
                {
                    if (!_encounterService.IsEnemy(kv.Value)
                        && _entityStats.HasEntity(kv.Key)
                        && _entityStats.GetCurrentHealth(kv.Key) > 0)
                        return kv.Key;
                }
            }
            return default;
        }

        private EntityId FindTauntEntity(IReadOnlyList<EntityId> candidates)
        {
            if (_passiveService == null) return default;
            for (int i = 0; i < candidates.Count; i++)
            {
                var entity = candidates[i];
                if (!_entityStats.HasEntity(entity) || _entityStats.GetCurrentHealth(entity) <= 0)
                    continue;
                if (_passiveService.HasTaunt(entity))
                    return entity;
            }
            return default;
        }
    }
}
