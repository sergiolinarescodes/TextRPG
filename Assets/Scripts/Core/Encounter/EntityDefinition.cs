using UnityEngine;

namespace TextRPG.Core.Encounter
{
    public sealed record PassiveEntry(
    string TriggerId,
    string TriggerParam,
    string EffectId,
    string EffectParam,
    int Value,
    string Target
);

    public sealed record EntityDefinition(
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
        PassiveEntry[] Passives = null,
        string[] Tags = null,
        string Description = null,
        string DeathReward = null,
        int DeathRewardValue = 0,
        int Tier = 1,
        int Dexterity = 0,
        int Constitution = 0
    );
}
