using System;
using System.Collections.Generic;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Lockpick;
using TextRPG.Core.Run;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordInput.Scenarios;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Services
{
    internal sealed class EventEncounterScope : IDisposable
    {
        public ICombatLoopService CombatLoop { get; private set; }
        public ScenarioEncounterAdapter EncounterAdapter { get; private set; }
        public IEventEncounterService EncounterService { get; private set; }

        private IDisposable _deathSubscription;
        private ICombatAIService _combatAI;

        internal static EventEncounterScope Create(RunSession session, EventEncounterDefinition encounter)
        {
            var scope = new EventEncounterScope();
            var s = session;

            scope.EncounterService = new EventEncounterService(
                s.EventBus, s.EntityStats, s.SlotService, s.CombatContext, s.ReactionService);
            s.ReactionContext.EncounterService = scope.EncounterService;

            // Wire lockpick service to encounter service (circular ref)
            if (s.LockpickService is LockpickService lockpick)
                lockpick.SetEncounterService(scope.EncounterService);

            // Encounter adapter (no auto-end — lifecycle managed by EventEncounterService)
            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(s.PlayerId);
            encounterAdapter.SetEventBus(s.EventBus);
            encounterAdapter.SetAutoEndOnAllDead(false);
            scope.EncounterAdapter = encounterAdapter;

            // Register interactable UnitDefinitions for rendering
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");
                var uid = new UnitId(entityId.Value);
                s.UnitService.Register(uid,
                    new UnitDefinition(uid, def.Name, def.MaxHealth, 0, 0, 0, def.Color));
            }

            scope.EncounterService.StartEncounter(encounter, s.PlayerId);

            // Register interactables as enemies in adapter (empty abilities → AI auto-passes)
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                var entityId = scope.EncounterService.InteractableEntities[i];
                var entityDef = new EntityDefinition(
                    def.Name, def.MaxHealth, 0, 0, 0, 0, 0, def.Color,
                    Array.Empty<string>(), 0, "interactable", def.Passives,
                    def.Tags, def.Description, def.DeathReward, def.DeathRewardValue);
                encounterAdapter.RegisterEnemy(entityId, entityDef);
            }
            encounterAdapter.Activate();

            // Register interactable passives
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var entityId = scope.EncounterService.InteractableEntities[i];
                    s.PassiveService.RegisterPassives(entityId, def.Passives);
                }
            }

            // CombatAI (interactables auto-pass since empty abilities)
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(s.StatusEffects);
            scope._combatAI = new CombatAIService(s.EventBus, encounterAdapter, s.EntityStats,
                s.TurnService, s.SlotService, s.CombatContext, s.ActionExecution, scorers,
                s.EnemyResolver as EnemyWordResolver, s.AllUnits, statusEffects: s.StatusEffects);

            // Turn order: player first, then interactables
            var turnOrder = new List<EntityId> { s.PlayerId };
            turnOrder.AddRange(scope.EncounterService.InteractableEntities);
            s.TurnService.SetTurnOrder(turnOrder);

            // CombatLoopService (same as combat scope)
            scope.CombatLoop = new CombatLoopService(
                s.EventBus, s.TurnService, s.EntityStats, s.WordResolver, s.WeaponService, s.PlayerId,
                s.ConsumableService, (IReservedWordHandler)s.RunService, s.CombatContext, s.WordCooldown, s.GiveValidator,
                statusEffects: s.StatusEffects, anxietyService: s.AnxietyService);
            ((CombatLoopService)scope.CombatLoop).Start();

            // Max player turns for event encounters
            ((EventEncounterService)scope.EncounterService).SetMaxPlayerTurns(2);

            // Mark dead entities in adapter
            scope._deathSubscription = s.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                encounterAdapter.MarkDead(evt.EntityId);
            });

            return scope;
        }

        public void Dispose()
        {
            _deathSubscription?.Dispose();
            _deathSubscription = null;
            (CombatLoop as IDisposable)?.Dispose();
            CombatLoop = null;
            (_combatAI as IDisposable)?.Dispose();
            _combatAI = null;
            if (EncounterService?.IsEncounterActive == true)
                EncounterService.EndEncounter();
            (EncounterService as IDisposable)?.Dispose();
            EncounterService = null;
            EncounterAdapter?.EndEncounter();
            EncounterAdapter = null;
        }
    }
}
