namespace TextRPG.Core.EncounterManager
{
    public interface ICombatModeService
    {
        bool IsInCombat { get; }
        void SetCombatMode(bool inCombat);
    }
}
