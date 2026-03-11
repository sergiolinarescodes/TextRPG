using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Run;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput.Scenarios;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Services
{
    internal sealed class CombatEncounterScope : IDisposable
    {
        public ICombatLoopService CombatLoop { get; private set; }
        public ICombatAIService CombatAI { get; private set; }
        public ScenarioEncounterAdapter EncounterAdapter { get; private set; }

        private IDisposable _recruitSubscription;
        private IDisposable _deathSubscription;

        internal static CombatEncounterScope Create(RunSession session, EncounterDefinition encounter)
        {
            var scope = new CombatEncounterScope();
            var s = session;

            (s.EnemyResolver as EnemyWordResolver)?.Clear();

            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(s.PlayerId);
            encounterAdapter.SetEventBus(s.EventBus);
            scope.EncounterAdapter = encounterAdapter;

            var enemyIds = new List<EntityId>();
            for (int i = 0; i < encounter.Enemies.Length && i < 3; i++)
            {
                var def = encounter.Enemies[i];
                var unitId = $"{def.Name.ToLowerInvariant()}_{i}";
                var entityId = new EntityId(unitId);

                s.EntityStats.RegisterEntity(entityId, def.MaxHealth, def.Strength, def.MagicPower,
                    def.PhysicalDefense, def.MagicDefense, def.Luck);
                s.UnitService.Register(new UnitId(unitId),
                    new UnitDefinition(new UnitId(unitId), def.Name,
                        def.MaxHealth, def.Strength, def.PhysicalDefense, def.Luck, def.Color));
                s.SlotService.RegisterEnemy(entityId, i);
                enemyIds.Add(entityId);
                encounterAdapter.RegisterEnemy(entityId, def);

                var matchKey = s.AllUnits.FirstOrDefault(kvp =>
                    kvp.Value.Name == def.Name).Key;
                if (matchKey != null)
                    UnitDatabaseLoader.RegisterUnitWords(s.EnemyResolver as EnemyWordResolver, matchKey);
            }
            encounterAdapter.Activate();

            ((CombatContext)s.CombatContext).SetEnemies(enemyIds.ToArray());
            ((CombatContext)s.CombatContext).SetAllies(Array.Empty<EntityId>());

            var scorers = CombatAISystemInstaller.CreateScorerRegistry(s.StatusEffects);

            scope.CombatAI = new CombatAIService(s.EventBus, encounterAdapter, s.EntityStats,
                s.TurnService, s.SlotService, s.CombatContext, s.ActionExecution, scorers,
                s.EnemyResolver as EnemyWordResolver, s.AllUnits, statusEffects: s.StatusEffects);

            var turnOrder = new List<EntityId> { s.PlayerId };
            turnOrder.AddRange(enemyIds);
            s.TurnService.SetTurnOrder(turnOrder);

            scope.CombatLoop = new CombatLoopService(
                s.EventBus, s.TurnService, s.EntityStats, s.WordResolver, s.WeaponService, s.PlayerId,
                s.ConsumableService, (IReservedWordHandler)s.RunService, s.CombatContext, s.WordCooldown, s.GiveValidator,
                statusEffects: s.StatusEffects, anxietyService: s.AnxietyService);
            ((CombatLoopService)scope.CombatLoop).Start();

            s.ExperienceService?.SetEncounterService(encounterAdapter);

            // Register passives and tags for enemies
            for (int i = 0; i < encounter.Enemies.Length && i < 3; i++)
            {
                var def = encounter.Enemies[i];
                var eid = new EntityId($"{def.Name.ToLowerInvariant()}_{i}");
                if (def.Passives != null)
                    s.PassiveService.RegisterPassives(eid, def.Passives);
                if (def.Tags != null && def.Tags.Length > 0)
                    s.ReactionService.RegisterReactions(eid, null, def.Tags);
            }

            // Mark dead enemies
            scope._deathSubscription = s.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                encounterAdapter.MarkDead(evt.EntityId);
            });

            // Recruitment: unregister enemy when recruited
            scope._recruitSubscription = s.EventBus.Subscribe<EntityRecruitedEvent>(evt =>
            {
                encounterAdapter.UnregisterEnemy(evt.EntityId);
            });

            return scope;
        }

        public void Dispose()
        {
            _deathSubscription?.Dispose();
            _deathSubscription = null;
            _recruitSubscription?.Dispose();
            _recruitSubscription = null;
            (CombatLoop as IDisposable)?.Dispose();
            CombatLoop = null;
            (CombatAI as IDisposable)?.Dispose();
            CombatAI = null;
            EncounterAdapter?.EndEncounter();
            EncounterAdapter = null;
        }
    }
}
