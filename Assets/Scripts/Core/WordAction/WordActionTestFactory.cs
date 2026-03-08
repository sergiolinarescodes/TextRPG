using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using Unidad.Core.Testing;
using UnityEngine;

namespace TextRPG.Core.WordAction
{
    internal sealed class WordActionTestFactory : ISystemTestFactory
    {
        public Type[] TestedServices => new[] { typeof(IWordResolver), typeof(IActionRegistry), typeof(IWordTagResolver) };

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
            registry.Register("Water", new ActionDefinition("Water", "Water", new Color(0.2f, 0.5f, 1f)));
            registry.Register("Fire", new ActionDefinition("Fire", "Fire", new Color(1f, 0.4f, 0.1f)));
            registry.Register("Push", new ActionDefinition("Push", "Push", new Color(0.8f, 0.8f, 0.2f)));
            registry.Register("Damage", new ActionDefinition("Damage", "Damage", new Color(1f, 0.2f, 0.2f)));
            registry.Register("Heal", new ActionDefinition("Heal", "Heal", new Color(0.2f, 0.9f, 0.3f)));
            registry.Register("Burn", new ActionDefinition("Burn", "Burn", new Color(1f, 0.5f, 0.2f)));
            registry.Register("Shock", new ActionDefinition("Shock", "Shock", new Color(1f, 1f, 0.3f)));
            registry.Register("Fear", new ActionDefinition("Fear", "Fear", new Color(0.5f, 0.1f, 0.5f)));
            registry.Register("Stun", new ActionDefinition("Stun", "Stun", new Color(0.9f, 0.9f, 0.1f)));
            registry.Register("Freeze", new ActionDefinition("Freeze", "Freeze", new Color(0.5f, 0.8f, 1f)));
            registry.Register("Concussion", new ActionDefinition("Concussion", "Concussion", new Color(0.7f, 0.5f, 0.2f)));
            registry.Register("BuffStrength", new ActionDefinition("BuffStrength", "Buff Strength", new Color(0.3f, 0.9f, 0.3f)));
            registry.Register("BuffMagicPower", new ActionDefinition("BuffMagicPower", "Buff Magic Power", new Color(0.4f, 0.6f, 1f)));
            registry.Register("BuffPhysicalDefense", new ActionDefinition("BuffPhysicalDefense", "Buff Phys Def", new Color(0.7f, 0.7f, 0.3f)));
            registry.Register("BuffMagicDefense", new ActionDefinition("BuffMagicDefense", "Buff Magic Def", new Color(0.5f, 0.5f, 1f)));
            registry.Register("BuffLuck", new ActionDefinition("BuffLuck", "Buff Luck", new Color(0.9f, 0.8f, 0.2f)));
            registry.Register("DebuffStrength", new ActionDefinition("DebuffStrength", "Debuff Strength", new Color(0.6f, 0.2f, 0.2f)));
            registry.Register("DebuffMagicPower", new ActionDefinition("DebuffMagicPower", "Debuff Magic Power", new Color(0.4f, 0.2f, 0.6f)));
            registry.Register("DebuffPhysicalDefense", new ActionDefinition("DebuffPhysicalDefense", "Debuff Phys Def", new Color(0.5f, 0.3f, 0.1f)));
            registry.Register("DebuffMagicDefense", new ActionDefinition("DebuffMagicDefense", "Debuff Magic Def", new Color(0.3f, 0.2f, 0.5f)));
            registry.Register("DebuffLuck", new ActionDefinition("DebuffLuck", "Debuff Luck", new Color(0.5f, 0.4f, 0.1f)));

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
                },
                ["bandage"] = new()
                {
                    new WordActionMapping("Heal", 2),
                },
                ["abyss"] = new()
                {
                    new WordActionMapping("Damage", 3),
                },
                ["spark"] = new()
                {
                    new WordActionMapping("Fire", 2),
                    new WordActionMapping("Damage", 1),
                },
                ["absorb"] = new()
                {
                    new WordActionMapping("Damage", 1, "SingleEnemy", 3, AreaShape.Single),
                    new WordActionMapping("Heal", 1, "Self", 0, AreaShape.Single),
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
                ["absorb"] = new WordMeta("SingleEnemy", 0),
            };

            var wordTags = new Dictionary<string, List<string>>
            {
                ["tsunami"] = new() { "NATURE", "ELEMENTAL", "OFFENSIVE" },
                ["ember"] = new() { "ELEMENTAL", "OFFENSIVE" },
                ["inferno"] = new() { "ELEMENTAL", "OFFENSIVE" },
                ["bandage"] = new() { "RESTORATION" },
                ["abyss"] = new() { "SHADOW", "OFFENSIVE" },
                ["spark"] = new() { "ELEMENTAL", "OFFENSIVE" },
                ["absorb"] = new() { "SHADOW", "RESTORATION" },
            };

            var ammoWordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ammoResolver = new WordResolver(
                new Dictionary<string, List<WordActionMapping>>(),
                new Dictionary<string, WordMeta>());

            var tagResolver = new WordTagResolver(wordTags);
            var resolver = new WordResolver(mappings, meta);
            return new WordActionData(resolver, registry, tagResolver, ammoWordSet, ammoResolver);
        }
    }
}
