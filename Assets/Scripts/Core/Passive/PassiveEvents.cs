using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public readonly record struct PassiveTriggeredEvent(
        EntityId SourceEntity, string TriggerId, string EffectId,
        int Value, EntityId? AffectedEntity);
}
