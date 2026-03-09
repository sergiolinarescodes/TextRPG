using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IPassiveService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStats.EntityStatsService(deps.EventBus);
            var slotService = new CombatSlot.CombatSlotService(deps.EventBus);
            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry();
            var targetResolver = new PassiveTargetResolver();
            var context = new PassiveContext(entityStats, slotService, deps.EventBus, null);
            return new PassiveService(deps.EventBus, triggerRegistry, effectRegistry, targetResolver, context);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.PassiveScenario();
            yield return new Scenarios.PassiveVerificationScenario();
        }
    }
}
