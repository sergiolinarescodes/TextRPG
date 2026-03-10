using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Consumable
{
    public readonly record struct ConsumableEquippedEvent(EntityId Entity, ConsumableDefinition Consumable);
    public readonly record struct ConsumableDurabilityChangedEvent(EntityId Entity, string Word, int CurrentDurability, int MaxDurability);
    public readonly record struct ConsumableDestroyedEvent(EntityId Entity, string Word);
    public readonly record struct ConsumableAmmoSubmittedEvent(EntityId Source, string AmmoWord);
}
