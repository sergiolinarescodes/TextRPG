using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public readonly record struct StatusEffectAppliedEvent(EntityId Target, StatusEffectType Type, int Duration, EntityId Source);
    public readonly record struct StatusEffectRemovedEvent(EntityId Target, StatusEffectType Type);
    public readonly record struct StatusEffectTickedEvent(EntityId Target, StatusEffectType Type, int RemainingDuration);
    public readonly record struct StatusEffectExpiredEvent(EntityId Target, StatusEffectType Type);
    public readonly record struct StatusEffectDamageEvent(EntityId Target, StatusEffectType Type, int Damage);
}
