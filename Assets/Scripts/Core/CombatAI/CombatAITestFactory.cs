using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatAI
{
    internal sealed class CombatAITestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ICombatAIService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new TextRPG.Core.EntityStats.EntityStatsService(deps.EventBus);
            var turnService = new TextRPG.Core.TurnSystem.TurnService(deps.EventBus);
            var slotService = new TextRPG.Core.CombatSlot.CombatSlotService(deps.EventBus);
            var combatContext = new TextRPG.Core.ActionExecution.CombatContext();
            var enemyResolver = new TextRPG.Core.Encounter.EnemyWordResolver();

            var effectHandlerRegistry = TextRPG.Core.StatusEffect.StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new TextRPG.Core.StatusEffect.Handlers.StatusEffectHandlerContext(entityStats, turnService, deps.EventBus);
            var statusEffects = new TextRPG.Core.StatusEffect.StatusEffectService(deps.EventBus, entityStats, turnService, effectHandlerRegistry, handlerContext);
            handlerContext.StatusEffects = statusEffects;

            var encounterService = new TextRPG.Core.Encounter.EncounterService(deps.EventBus, entityStats, turnService, slotService, combatContext, enemyResolver);

            var actionHandlerRegistry = TextRPG.Core.ActionExecution.ActionExecutionTestFactory.CreateHandlerRegistry(deps.EventBus, entityStats, statusEffects, combatContext);
            var wordData = TextRPG.Core.WordAction.WordActionTestFactory.CreateTestData();
            var actionExecution = new TextRPG.Core.ActionExecution.ActionExecutionService(deps.EventBus, wordData.Resolver, actionHandlerRegistry, combatContext);

            var scorers = CombatAISystemInstaller.CreateScorerRegistry(statusEffects);

            return new CombatAIService(deps.EventBus, encounterService, entityStats, turnService, slotService, combatContext, actionExecution, scorers, enemyResolver);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.CombatAIScenario();
        }
    }
}
