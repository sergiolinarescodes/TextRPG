using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public readonly record struct PassiveTriggeredEvent(
        EntityId SourceEntity, string PassiveId, int Value, EntityId? AffectedEntity);
}
