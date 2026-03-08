namespace TextRPG.Core.EntityStats
{
    public readonly record struct EntityRegisteredEvent(EntityId EntityId, int MaxHealth);
    public readonly record struct EntityRemovedEvent(EntityId EntityId);
    public readonly record struct DamageTakenEvent(EntityId EntityId, int Amount, int RemainingHealth, EntityId? DamageSource = null);
    public readonly record struct HealedEvent(EntityId EntityId, int Amount, int NewHealth);
    public readonly record struct EntityDiedEvent(EntityId EntityId);
    public readonly record struct StatModifierAddedEvent(EntityId EntityId, StatType Stat, string ModifierId);
    public readonly record struct StatModifierRemovedEvent(EntityId EntityId, StatType Stat, string ModifierId);
    public readonly record struct ShieldChangedEvent(EntityId EntityId, int CurrentShield, int PreviousShield);
    public readonly record struct ManaChangedEvent(EntityId EntityId, int CurrentMana, int PreviousMana);
    public readonly record struct ManaInsufficientEvent(EntityId EntityId, int Cost, int CurrentMana);
}
