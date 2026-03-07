using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.TurnSystem
{
    internal sealed class TurnTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ITurnService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new TurnService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.TurnFlowScenario();
        }
    }
}
