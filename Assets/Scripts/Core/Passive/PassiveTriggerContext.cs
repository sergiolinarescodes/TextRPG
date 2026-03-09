using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public readonly record struct PassiveTriggerContext(
        EntityId Owner,
        EntityId? EventEntity,
        EntityId? EventSource,
        string Word
    );
}
