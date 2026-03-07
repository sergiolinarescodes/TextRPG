using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.EntityStats
{
    internal sealed class EntityStatsTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEntityStatsService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new EntityStatsService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.EntityStatsScenario();
        }
    }
}
