using System;
using System.Collections.Generic;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordAction
{
    internal sealed class WordActionTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IWordResolver), typeof(IActionRegistry) };

        public object CreateForTesting(TestDependencies deps)
        {
            return CreateTestData();
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.WordActionLookupScenario();
        }

        internal static WordActionData CreateTestData()
        {
            var registry = new ActionRegistry();
            registry.Register("Water", new ActionDefinition("Water", "Water"));
            registry.Register("Fire", new ActionDefinition("Fire", "Fire"));
            registry.Register("Push", new ActionDefinition("Push", "Push"));
            registry.Register("Damage", new ActionDefinition("Damage", "Damage"));
            registry.Register("Heal", new ActionDefinition("Heal", "Heal"));
            registry.Register("Light", new ActionDefinition("Light", "Light"));
            registry.Register("Dark", new ActionDefinition("Dark", "Dark"));
            registry.Register("Burn", new ActionDefinition("Burn", "Burn"));

            var mappings = new Dictionary<string, List<WordActionMapping>>
            {
                ["tsunami"] = new()
                {
                    new WordActionMapping("Water", 5),
                    new WordActionMapping("Damage", 5),
                    new WordActionMapping("Push", 2),
                },
                ["ember"] = new()
                {
                    new WordActionMapping("Fire", 1),
                    new WordActionMapping("Damage", 1),
                },
                ["inferno"] = new()
                {
                    new WordActionMapping("Fire", 5),
                    new WordActionMapping("Damage", 4),
                    new WordActionMapping("Burn", 4),
                    new WordActionMapping("Light", 2),
                },
                ["bandage"] = new()
                {
                    new WordActionMapping("Heal", 2),
                },
                ["abyss"] = new()
                {
                    new WordActionMapping("Dark", 5),
                    new WordActionMapping("Damage", 3),
                },
                ["spark"] = new()
                {
                    new WordActionMapping("Fire", 2),
                    new WordActionMapping("Light", 1),
                    new WordActionMapping("Damage", 1),
                },
            };

            var meta = new Dictionary<string, WordMeta>
            {
                ["tsunami"] = new WordMeta("AreaAll", 6),
                ["ember"] = new WordMeta("SingleEnemy", 0),
                ["inferno"] = new WordMeta("AreaEnemies", 5),
                ["bandage"] = new WordMeta("Self", 0),
                ["abyss"] = new WordMeta("AreaAll", 4),
                ["spark"] = new WordMeta("SingleEnemy", 0),
            };

            var resolver = new WordResolver(mappings, meta);
            return new WordActionData(resolver, registry);
        }
    }
}
