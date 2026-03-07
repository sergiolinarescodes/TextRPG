using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.Testing;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class ActionExecutionTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IActionExecutionService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var turnService = new TurnService(deps.EventBus);
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(entityStats, turnService, deps.EventBus);
            var statusEffects = new StatusEffectService(deps.EventBus, entityStats, turnService, effectHandlerRegistry, handlerContext);
            handlerContext.StatusEffects = statusEffects;
            var wordData = WordActionTestFactory.CreateTestData();
            var combatContext = new CombatContext();
            var handlerRegistry = CreateHandlerRegistry(deps.EventBus, entityStats, statusEffects, combatContext);
            return new ActionExecutionService(deps.EventBus, wordData.Resolver, handlerRegistry, combatContext);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.ActionExecutionScenario();
            yield return new Scenarios.WordActionCompositionScenario();
            yield return new Scenarios.CombatActionVerificationScenario();
        }

        internal static ActionHandlerRegistry CreateHandlerRegistry(
            Unidad.Core.EventBus.IEventBus eventBus,
            IEntityStatsService entityStats,
            IStatusEffectService statusEffects,
            ICombatContext combatContext = null,
            ITurnService turnService = null)
        {
            combatContext ??= new CombatContext();
            var ctx = new ActionHandlerContext(entityStats, eventBus, combatContext,
                statusEffects, turnService);
            return ActionHandlerRegistryFactory.CreateDefault(ctx);
        }
    }
}
