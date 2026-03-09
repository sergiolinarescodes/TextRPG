using TextRPG.Core.Encounter;
using UnityEngine;

namespace TextRPG.Core.Equipment
{
    public sealed record EquipmentItemDefinition(
        string ItemWord,
        string DisplayName,
        EquipmentSlotType SlotType,
        int Durability,
        StatBonus Stats,
        Color Color,
        string[] AmmoWords,
        PassiveEntry[] Passives);
}
