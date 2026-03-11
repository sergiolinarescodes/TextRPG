using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Experience
{
    public readonly record struct ExperienceGainedEvent(
        EntityId KilledEntity, int XpAmount, int TotalXp, int XpForNextLevel, int CurrentLevel);

    public readonly record struct LevelUpEvent(int NewLevel, int PreviousLevel);
}
