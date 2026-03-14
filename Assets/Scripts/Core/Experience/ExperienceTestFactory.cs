using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.Experience
{
    internal sealed class ExperienceTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IExperienceService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new ExperienceService(deps.EventBus);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.ExperienceScenario();
        }
    }
}
