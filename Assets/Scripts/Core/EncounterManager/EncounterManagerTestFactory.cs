using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Testing;

namespace TextRPG.Core.EncounterManager
{
    internal sealed class EncounterManagerTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEncounterManager), typeof(ICombatModeService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var slotService = new CombatSlotService(deps.EventBus);
            var combatContext = new CombatContext();
            var turnService = new TurnService(deps.EventBus);
            var enemyResolver = new EnemyWordResolver();

            var combatEncounter = new EncounterService(
                deps.EventBus, entityStats, turnService, slotService, combatContext, enemyResolver);

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(entityStats, slotService, deps.EventBus, null);
            var reactionService = new ReactionService(deps.EventBus, outcomeRegistry, encounterContext, tagReactions);
            var eventEncounter = new EventEncounterService(
                deps.EventBus, entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = eventEncounter;

            var providerRegistry = EventEncounterSystemInstaller.CreateProviderRegistry();
            return new EncounterManager(deps.EventBus, combatEncounter, eventEncounter, providerRegistry);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EncounterManagerScenario();
        }
    }
}
