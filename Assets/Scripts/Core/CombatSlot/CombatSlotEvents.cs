using TextRPG.Core.EntityStats;

namespace TextRPG.Core.CombatSlot
{
    public readonly record struct SlotEntityRegisteredEvent(EntityId EntityId, CombatSlot Slot);
    public readonly record struct SlotEntityRemovedEvent(EntityId EntityId, CombatSlot Slot);
}
