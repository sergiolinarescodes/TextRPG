using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Testing;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IStatusEffectService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var turnService = new TurnService(deps.EventBus);
            var handlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(entityStats, turnService, deps.EventBus);
            var service = new StatusEffectService(deps.EventBus, entityStats, turnService, handlerRegistry, handlerContext);
            handlerContext.StatusEffects = service;
            return service;
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.StatusEffectScenario();
        }
    }
}
