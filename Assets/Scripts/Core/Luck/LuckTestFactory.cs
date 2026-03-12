using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.Testing;

namespace TextRPG.Core.Luck
{
    internal sealed class LuckTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ILuckService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            return new LuckService(entityStats);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.LuckVerificationScenario();
        }
    }
}
