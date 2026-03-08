using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatLoop
{
    internal sealed class CombatLoopTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(ICombatLoopService) };

        public object CreateForTesting(TestDependencies deps)
        {
            var entityStats = new EntityStatsService(deps.EventBus);
            var turnService = new TurnService(deps.EventBus);
            var wordData = WordActionTestFactory.CreateTestData();
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordData);
            var weaponService = new WeaponService(deps.EventBus, weaponRegistry);
            var playerId = new EntityId("player");

            return new CombatLoopService(
                deps.EventBus, turnService, entityStats, wordData.Resolver, weaponService, playerId);
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.CombatLoopScenario();
        }
    }
}
