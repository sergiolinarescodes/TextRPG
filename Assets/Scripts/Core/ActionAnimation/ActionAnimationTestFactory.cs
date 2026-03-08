using System;
using System.Collections.Generic;
using Unidad.Core.Abstractions;
using Unidad.Core.Testing;

namespace TextRPG.Core.ActionAnimation
{
    internal sealed class ActionAnimationTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IActionAnimationService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return new ActionAnimationService(deps.EventBus, new InstantAnimationResolver());
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.ActionAnimationScenario();
        }
    }
}
