using UnityEngine;

namespace TextRPG.Core.Encounter
{
    public sealed record EnemyDefinition(
        string Name,
        int MaxHealth,
        int Strength,
        int MagicPower,
        int PhysicalDefense,
        int MagicDefense,
        int Luck,
        int MovementPoints,
        Color Color,
        string[] Abilities
    );
}
