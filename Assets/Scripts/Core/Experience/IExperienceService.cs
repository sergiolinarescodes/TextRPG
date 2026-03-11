namespace TextRPG.Core.Experience
{
    public interface IExperienceService
    {
        int CurrentLevel { get; }
        int CurrentXp { get; }
        int XpForNextLevel { get; }
        float XpProgress { get; }
    }
}
