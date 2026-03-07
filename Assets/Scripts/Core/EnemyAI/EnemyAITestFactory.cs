using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.EnemyAI
{
    internal sealed class EnemyAITestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEnemyAIService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new TextRPG.Core.EntityStats.EntityStatsService(deps.EventBus);
            var turnService = new TextRPG.Core.TurnSystem.TurnService(deps.EventBus);
            var unitService = new TextRPG.Core.UnitRendering.UnitService(deps.EventBus);
            var combatGrid = new TextRPG.Core.CombatGrid.CombatGridService(deps.EventBus, unitService);
            var combatContext = new TextRPG.Core.ActionExecution.CombatContext();
            var enemyResolver = new TextRPG.Core.Encounter.EnemyWordResolver();

            var effectHandlerRegistry = TextRPG.Core.StatusEffect.StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new TextRPG.Core.StatusEffect.Handlers.StatusEffectHandlerContext(entityStats, turnService, deps.EventBus);
            var statusEffects = new TextRPG.Core.StatusEffect.StatusEffectService(deps.EventBus, entityStats, turnService, effectHandlerRegistry, handlerContext);
            handlerContext.StatusEffects = statusEffects;

            var encounterService = new TextRPG.Core.Encounter.EncounterService(deps.EventBus, entityStats, turnService, combatGrid, combatContext, enemyResolver);

            var actionHandlerRegistry = TextRPG.Core.ActionExecution.ActionExecutionTestFactory.CreateHandlerRegistry(deps.EventBus, entityStats, statusEffects, combatContext);
            var wordData = TextRPG.Core.WordAction.WordActionTestFactory.CreateTestData();
            var actionExecution = new TextRPG.Core.ActionExecution.ActionExecutionService(deps.EventBus, wordData.Resolver, actionHandlerRegistry, combatContext);

            var scorers = EnemyAISystemInstaller.CreateScorerRegistry(statusEffects);

            return new EnemyAIService(deps.EventBus, encounterService, entityStats, turnService, combatGrid, combatContext, actionExecution, scorers, enemyResolver);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EnemyAIScenario();
        }
    }
}
