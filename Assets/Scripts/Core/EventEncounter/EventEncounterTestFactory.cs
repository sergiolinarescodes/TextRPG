using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using Unidad.Core.Testing;

namespace TextRPG.Core.EventEncounter
{
    internal sealed class EventEncounterTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEventEncounterService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var slotService = new CombatSlotService(deps.EventBus);
            var combatContext = new CombatContext();

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry(null);
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(entityStats, slotService, deps.EventBus, null);
            var reactionService = new ReactionService(deps.EventBus, outcomeRegistry, encounterContext, tagReactions);
            var service = new EventEncounterService(deps.EventBus, entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = service;

            return service;
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EventEncounterScenario();
        }
    }
}
