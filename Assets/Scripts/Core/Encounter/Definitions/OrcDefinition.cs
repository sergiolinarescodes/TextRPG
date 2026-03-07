using UnityEngine;

namespace TextRPG.Core.Encounter.Definitions
{
    internal static class OrcDefinition
    {
        public static EnemyDefinition Create() => new(
            Name: "ORC",
            MaxHealth: 20,
            Strength: 8,
            MagicPower: 0,
            PhysicalDefense: 4,
            MagicDefense: 2,
            Luck: 1,
            MovementPoints: 2,
            Color: Color.green,
            Abilities: new[] { "SHOUT", "MACE" }
        );

        public static void RegisterWords(EnemyWordResolver resolver)
        {
            resolver.RegisterWord("shout",
                new() { new TextRPG.Core.WordAction.WordActionMapping("Fear", 2) },
                new TextRPG.Core.WordAction.WordMeta("AreaEnemies", 0));

            resolver.RegisterWord("mace",
                new() { new TextRPG.Core.WordAction.WordActionMapping("Damage", 2) },
                new TextRPG.Core.WordAction.WordMeta("Melee", 0));
        }
    }
}
