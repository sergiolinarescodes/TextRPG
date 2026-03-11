using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.WordAction;
using Unidad.Core.Testing;

namespace TextRPG.Core.EventEncounterLoop
{
    internal sealed class EventEncounterLoopTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEventEncounterLoopService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var slotService = new CombatSlotService(deps.EventBus);
            var combatContext = new CombatContext();
            var wordData = WordActionTestFactory.CreateTestData();

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry(null);
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(entityStats, slotService, deps.EventBus, null);
            var reactionService = new ReactionService(deps.EventBus, outcomeRegistry, encounterContext, tagReactions);
            var encounterService = new EventEncounterService(
                deps.EventBus, entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = encounterService;

            var playerId = new EntityId("player");
            return new EventEncounterLoopService(
                deps.EventBus, entityStats, wordData.Resolver, encounterService, playerId);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EventEncounterLoopScenario();
            yield return new Scenarios.EventEncounterLiveScenario();
        }
    }
}
