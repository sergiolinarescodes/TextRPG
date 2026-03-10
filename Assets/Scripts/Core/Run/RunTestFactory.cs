using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.Run
{
    internal sealed class RunTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IRunService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new RunService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.RunMapScenario();
        }
    }
}
