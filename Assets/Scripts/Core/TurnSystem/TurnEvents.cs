using TextRPG.Core.EntityStats;

namespace TextRPG.Core.TurnSystem
{
    public readonly record struct TurnStartedEvent(EntityId EntityId, int TurnNumber);
    public readonly record struct TurnEndedEvent(EntityId EntityId, int TurnNumber);
    public readonly record struct RoundStartedEvent(int RoundNumber);
    public readonly record struct RoundEndedEvent(int RoundNumber);
    public readonly record struct ExtraTurnGrantedEvent(EntityId EntityId);
}
