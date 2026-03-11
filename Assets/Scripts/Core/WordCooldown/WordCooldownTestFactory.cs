using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordCooldown
{
    internal sealed class WordCooldownTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IWordCooldownService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new WordCooldownService();
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.GiveCooldownLiveScenario();
        }
    }
}
