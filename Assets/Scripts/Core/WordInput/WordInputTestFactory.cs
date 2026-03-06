using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordInput
{
    internal sealed class WordInputTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IWordInputService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new WordInputService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.WordInputScenario();
        }
    }
}
