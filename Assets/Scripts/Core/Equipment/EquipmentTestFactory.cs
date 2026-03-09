using System;
using System.Collections.Generic;
using TextRPG.Core.Equipment.Scenarios;
using Unidad.Core.Testing;

namespace TextRPG.Core.Equipment
{
    internal sealed class EquipmentTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IEquipmentService), typeof(IItemRegistry) };

        public object CreateForTesting(TestDependencies deps)
        {
            return null;
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new EquipmentScenario();
        }
    }
}
