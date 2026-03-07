using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.WordAction;
using Unidad.Core.Testing;

namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IWeaponService), typeof(IWeaponActionExecutor) };

        public object CreateForTesting(TestDependencies deps)
        {
            return CreateTestWordActionData();
        }

        public IEnumerable<ITestScenario> GetScenarios()
        {
            yield return new Scenarios.WeaponScenario();
        }

        internal static WordActionData CreateTestWordActionData()
        {
            var registry = new ActionRegistry();
            registry.Register("Weapon", new ActionDefinition("Weapon", "Weapon", UnityEngine.Color.yellow));
            registry.Register("Damage", new ActionDefinition("Damage", "Damage", UnityEngine.Color.red));

            var mappings = new Dictionary<string, List<WordActionMapping>>
            {
                ["gun"] = new()
                {
                    new WordActionMapping("Weapon", 4, "Self", null, null, "9mm"),
                    new WordActionMapping("Weapon", 4, "Self", null, null, "buckshot"),
                },
                ["sword"] = new()
                {
                    new WordActionMapping("Weapon", 5, "Self", null, null, "slash"),
                    new WordActionMapping("Weapon", 5, "Self", null, null, "stab"),
                },
                ["9mm"] = new()
                {
                    new WordActionMapping("Damage", 3, "SingleEnemy", 5, AreaShape.Single),
                },
                ["buckshot"] = new()
                {
                    new WordActionMapping("Damage", 4, "SingleEnemy", 3, AreaShape.Cross),
                },
                ["slash"] = new()
                {
                    new WordActionMapping("Damage", 3, "Melee", 1, AreaShape.Single),
                },
                ["stab"] = new()
                {
                    new WordActionMapping("Damage", 4, "SingleEnemy", 1, AreaShape.Single),
                },
            };

            var meta = new Dictionary<string, WordMeta>
            {
                ["gun"] = new WordMeta("Self", 0),
                ["sword"] = new WordMeta("Self", 0),
                ["9mm"] = new WordMeta("SingleEnemy", 0, 5, AreaShape.Single),
                ["buckshot"] = new WordMeta("SingleEnemy", 0, 3, AreaShape.Cross),
                ["slash"] = new WordMeta("Melee", 0, 1, AreaShape.Single),
                ["stab"] = new WordMeta("SingleEnemy", 0, 1, AreaShape.Single),
            };

            var ammoWordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "9mm", "buckshot", "slash", "stab" };

            var ammoMappings = new Dictionary<string, List<WordActionMapping>>
            {
                ["9mm"] = mappings["9mm"],
                ["buckshot"] = mappings["buckshot"],
                ["slash"] = mappings["slash"],
                ["stab"] = mappings["stab"],
            };
            var ammoMeta = new Dictionary<string, WordMeta>
            {
                ["9mm"] = meta["9mm"],
                ["buckshot"] = meta["buckshot"],
                ["slash"] = meta["slash"],
                ["stab"] = meta["stab"],
            };
            var ammoResolver = new WordResolver(ammoMappings, ammoMeta);

            var tagResolver = new WordTagResolver(new Dictionary<string, List<string>>());
            var resolver = new WordResolver(mappings, meta);
            return new WordActionData(resolver, registry, tagResolver, ammoWordSet, ammoResolver);
        }

        internal static IWeaponRegistry CreateTestWeaponRegistry(WordActionData data)
        {
            return WeaponSystemInstaller.BuildWeaponRegistry(data);
        }
    }
}
