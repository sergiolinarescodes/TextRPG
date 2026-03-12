using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.Testing;

namespace TextRPG.Core.Lockpick
{
    internal sealed class LockpickTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ILockpickService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var luckService = new LuckService(entityStats);
            return new LockpickService(deps.EventBus, entityStats, luckService);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield break;
        }
    }
}
