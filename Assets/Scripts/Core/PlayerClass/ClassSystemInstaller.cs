using System;
using System.Collections.Generic;
using TextRPG.Core.PlayerClass.Scenarios;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.PlayerClass
{
    public sealed class ClassSystemInstaller : ISystemInstaller
    {
        public void Install(Reflex.Core.ContainerBuilder builder)
        {
            // ClassService is created manually in RunSessionFactory, not via DI
        }

        public ISystemTestFactory CreateTestFactory() => new ClassTestFactory();
    }

    internal sealed class ClassTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IClassService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var playerId = new EntityStats.EntityId("test-player");
            return new ClassService(deps.EventBus, PlayerClass.Warrior, playerId, null);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new ClassScenario();
        }
    }
}
