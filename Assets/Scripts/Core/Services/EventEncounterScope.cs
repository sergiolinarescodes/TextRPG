using System;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.Lockpick;
using TextRPG.Core.Run;
using TextRPG.Core.UnitRendering;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Services
{
    internal sealed class EventEncounterScope : IDisposable
    {
        public IEventEncounterService EncounterService { get; private set; }
        public IEventEncounterLoopService LoopService { get; private set; }

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

            scope.LoopService = new EventEncounterLoopService(
                s.EventBus, s.EntityStats, s.WordResolver, scope.EncounterService, s.PlayerId,
                (IReservedWordHandler)s.RunService, s.CombatContext, s.WordCooldown, s.GiveValidator,
                consumableService: s.ConsumableService, maxInteractions: 2);
            ((EventEncounterLoopService)scope.LoopService).Start();

            return scope;
        }

        public void Dispose()
        {
            (LoopService as IDisposable)?.Dispose();
            LoopService = null;
            if (EncounterService?.IsEncounterActive == true)
                EncounterService.EndEncounter();
            (EncounterService as IDisposable)?.Dispose();
            EncounterService = null;
        }
    }
}
