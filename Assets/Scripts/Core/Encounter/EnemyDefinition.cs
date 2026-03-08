using UnityEngine;

namespace TextRPG.Core.Encounter
{
    public sealed record PassiveEntry(string PassiveId, int Value);

    public sealed record EnemyDefinition(
        string Name,
        int MaxHealth,
        int Strength,
        int MagicPower,
        int PhysicalDefense,
        int MagicDefense,
        int Luck,
        Color Color,
        string[] Abilities,
        int StartingShield = 0,
        string UnitType = "enemy",
        PassiveEntry[] Passives = null
    );
}
