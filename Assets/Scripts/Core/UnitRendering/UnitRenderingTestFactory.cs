using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class UnitRenderingTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IUnitService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new UnitService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.UnitRenderingScenario();
        }
    }
}
