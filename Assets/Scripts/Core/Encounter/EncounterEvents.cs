using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Encounter
{
    public readonly record struct EncounterStartedEvent(string EncounterId, int EnemyCount);
    public readonly record struct EncounterEndedEvent(string EncounterId, bool Victory);
    public readonly record struct EnemySpawnedEvent(EntityId EntityId, string EnemyName);
}
