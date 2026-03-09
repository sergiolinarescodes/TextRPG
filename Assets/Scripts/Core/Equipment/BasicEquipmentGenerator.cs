using System;
using TextRPG.Core.Encounter;
using UnityEngine;

namespace TextRPG.Core.Equipment
{
    internal static class BasicEquipmentGenerator
    {
        private static int _counter;

        private static readonly string[] HeadAdj = { "Iron", "Rusty", "Worn", "Copper", "Thick", "Padded" };
        private static readonly string[] HeadNoun = { "Helm", "Cap", "Hood", "Crown", "Visor", "Coif" };

        private static readonly string[] WearAdj = { "Leather", "Chain", "Torn", "Quilted", "Woven", "Heavy" };
        private static readonly string[] WearNoun = { "Vest", "Mail", "Cloak", "Tunic", "Shirt", "Robe" };

        private static readonly string[] AccAdj = { "Silver", "Bone", "Old", "Lucky", "Jade", "Amber" };
        private static readonly string[] AccNoun = { "Ring", "Charm", "Pendant", "Brooch", "Amulet", "Bead" };

        private static readonly Color HeadColor = new(0.6f, 0.7f, 0.8f);
        private static readonly Color WearColor = new(0.7f, 0.5f, 0.3f);
        private static readonly Color AccColor = new(1f, 0.85f, 0.3f);

        public static EquipmentItemDefinition[] GenerateRewards(System.Random rng = null)
        {
            rng ??= new System.Random();
            return new[]
            {
                Generate(EquipmentSlotType.Head, HeadAdj, HeadNoun, HeadColor, rng),
                Generate(EquipmentSlotType.Wear, WearAdj, WearNoun, WearColor, rng),
                Generate(EquipmentSlotType.Accessory, AccAdj, AccNoun, AccColor, rng),
            };
        }

        private static EquipmentItemDefinition Generate(
            EquipmentSlotType slot, string[] adjectives, string[] nouns, Color color, System.Random rng)
        {
            var adj = adjectives[rng.Next(adjectives.Length)];
            var noun = nouns[rng.Next(nouns.Length)];
            var displayName = $"{adj} {noun}";
            var itemWord = $"basic_{adj.ToLowerInvariant()}_{noun.ToLowerInvariant()}_{_counter++}";

            var stats = GenerateStats(rng);

            return new EquipmentItemDefinition(
                itemWord, displayName, slot, 0, stats, color,
                Array.Empty<string>(), Array.Empty<PassiveEntry>());
        }

        private static StatBonus GenerateStats(System.Random rng)
        {
            // Pick 2-3 random stats
            var statCount = rng.Next(2, 4);
            int strength = 0, magicPower = 0, physDefense = 0, magicDefense = 0, luck = 0, maxHealth = 0;

            // Available stat slots (indices)
            var available = new[] { 0, 1, 2, 3, 4, 5 };
            Shuffle(available, rng);

            for (int i = 0; i < statCount; i++)
            {
                switch (available[i])
                {
                    case 0: physDefense = rng.Next(1, 4); break;   // +1 to +3
                    case 1: maxHealth = rng.Next(2, 9); break;     // +2 to +8
                    case 2: strength = rng.Next(1, 3); break;      // +1 to +2
                    case 3: magicPower = rng.Next(1, 3); break;    // +1 to +2
                    case 4: magicDefense = rng.Next(1, 3); break;  // +1 to +2
                    case 5: luck = rng.Next(1, 3); break;          // +1 to +2
                }
            }

            return new StatBonus(strength, magicPower, physDefense, magicDefense, luck, maxHealth, 0);
        }

        private static void Shuffle(int[] array, System.Random rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
