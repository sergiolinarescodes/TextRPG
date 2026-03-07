using System;
using System.Collections.Generic;
using TextRPG.Core.UnitRendering;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatGrid
{
    internal sealed class CombatGridTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ICombatGridService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var unitService = new UnitService(deps.EventBus);
            return new CombatGridService(deps.EventBus, unitService);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.CombatGridScenario();
        }
    }
}
