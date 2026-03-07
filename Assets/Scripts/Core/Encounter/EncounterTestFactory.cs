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
            var unitService = new TextRPG.Core.UnitRendering.UnitService(deps.EventBus);
            var combatGrid = new TextRPG.Core.CombatGrid.CombatGridService(deps.EventBus, unitService);
            var combatContext = new TextRPG.Core.ActionExecution.CombatContext();
            var enemyResolver = new EnemyWordResolver();
            return new EncounterService(deps.EventBus, entityStats, turnService, combatGrid, combatContext, enemyResolver);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EncounterScenario();
        }
    }
}
