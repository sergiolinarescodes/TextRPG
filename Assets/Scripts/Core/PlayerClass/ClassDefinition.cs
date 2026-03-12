using TextRPG.Core.Encounter;
using UnityEngine;

namespace TextRPG.Core.PlayerClass
{
    public sealed record ClassDefinition(
        PlayerClass Class,
        string DisplayName,
        string Description,
        Color Color,
        int MaxHealth,
        int Strength,
        int MagicPower,
        int PhysicalDefense,
        int MagicDefense,
        int Luck,
        int MaxMana,
        int ManaRegen,
        int StartingMana,
        int Constitution,
        int Dexterity,
        PassiveEntry[] Passives,
        string[] PassiveDescriptions,
        string[] StartingItems);
}
