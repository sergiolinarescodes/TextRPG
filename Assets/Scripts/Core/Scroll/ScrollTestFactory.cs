using System;
using System.Collections.Generic;
using TextRPG.Core.Scroll.Scenarios;
using Unidad.Core.Testing;

namespace TextRPG.Core.Scroll
{
    internal sealed class ScrollTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ISpellService) };

        public object CreateForTesting(TestDependencies deps)
        {
            return null;
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new ScrollSpellScenario();
        }
    }
}
