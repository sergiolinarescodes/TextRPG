using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.Encounter
{
    internal sealed class EncounterTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEncounterService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new TextRPG.Core.EntityStats.EntityStatsService(deps.EventBus);
            var turnService = new TextRPG.Core.TurnSystem.TurnService(deps.EventBus);
            var slotService = new TextRPG.Core.CombatSlot.CombatSlotService(deps.EventBus);
            var combatContext = new TextRPG.Core.ActionExecution.CombatContext();
            var enemyResolver = new EnemyWordResolver();
            return new EncounterService(deps.EventBus, entityStats, turnService, slotService, combatContext, enemyResolver);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EncounterScenario();
        }
    }
}
