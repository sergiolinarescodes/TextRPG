using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Equipment
{
    public readonly record struct ItemEquippedEvent(EntityId Entity, EquipmentItemDefinition Item, EquipmentSlotType Slot);
    public readonly record struct ItemUnequippedEvent(EntityId Entity, EquipmentItemDefinition Item, EquipmentSlotType Slot);
    public readonly record struct ItemAcquiredEvent(EntityId Entity, string ItemWord);
}
