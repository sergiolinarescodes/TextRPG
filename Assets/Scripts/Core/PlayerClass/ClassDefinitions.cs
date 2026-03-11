using System;
using TextRPG.Core.Encounter;
using UnityEngine;

namespace TextRPG.Core.PlayerClass
{
    public static class ClassDefinitions
    {
        public static readonly ClassDefinition Mage = new(
            Class: PlayerClass.Mage,
            DisplayName: "Mage",
            Description: "A scholarly spellcaster who learns new scrolls as they grow.",
            Color: new Color(0.4f, 0.6f, 1f),
            MaxHealth: 30,
            Strength: 2,
            MagicPower: 8,
            PhysicalDefense: 1,
            MagicDefense: 4,
            Luck: 3,
            MaxMana: 12,
            ManaRegen: 3,
            StartingMana: 6,
            Constitution: 1,
            Passives: Array.Empty<PassiveEntry>(),
            PassiveDescriptions: new[] { "Arcane Scholar: Learn a random scroll on each level up" });

        public static readonly ClassDefinition Warrior = new(
            Class: PlayerClass.Warrior,
            DisplayName: "Warrior",
            Description: "A brute-force fighter who excels with melee attacks.",
            Color: new Color(1f, 0.4f, 0.3f),
            MaxHealth: 40,
            Strength: 8,
            MagicPower: 2,
            PhysicalDefense: 5,
            MagicDefense: 2,
            Luck: 3,
            MaxMana: 5,
            ManaRegen: 1,
            StartingMana: 3,
            Constitution: 1,
            Passives: Array.Empty<PassiveEntry>(),
            PassiveDescriptions: new[] { "Brute Force: MELEE actions deal 50% more damage" });

        public static readonly ClassDefinition Merchant = new(
            Class: PlayerClass.Merchant,
            DisplayName: "Merchant",
            Description: "A resourceful trader with golden luck and charming presence.",
            Color: new Color(1f, 0.85f, 0.2f),
            MaxHealth: 38,
            Strength: 4,
            MagicPower: 4,
            PhysicalDefense: 3,
            MagicDefense: 3,
            Luck: 6,
            MaxMana: 8,
            ManaRegen: 2,
            StartingMana: 4,
            Constitution: 1,
            Passives: new[]
            {
                new PassiveEntry("on_word_tag", "SOCIAL", "shield", null, 2, "Self")
            },
            PassiveDescriptions: new[]
            {
                "Golden Touch: Gain +1 bonus gold on every gold gain",
                "Charming Presence: Social words grant Shield(2)"
            });

        public static ClassDefinition Get(PlayerClass playerClass) => playerClass switch
        {
            PlayerClass.Mage => Mage,
            PlayerClass.Warrior => Warrior,
            PlayerClass.Merchant => Merchant,
            _ => throw new ArgumentOutOfRangeException(nameof(playerClass), playerClass, null)
        };
    }
}
