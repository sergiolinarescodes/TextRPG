using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatSlot
{
    internal sealed class CombatSlotTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ICombatSlotService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new CombatSlotService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.CombatSlotScenario();
        }
    }
}
