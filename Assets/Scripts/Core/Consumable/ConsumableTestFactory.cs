using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.Consumable
{
    internal sealed class ConsumableTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IConsumableService), typeof(IConsumableRegistry) };

        public object CreateForTesting(TestDependencies deps)
        {
            return null;
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield break;
        }
    }
}
