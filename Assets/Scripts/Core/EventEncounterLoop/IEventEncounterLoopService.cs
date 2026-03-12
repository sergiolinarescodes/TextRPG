using TextRPG.Core.CombatLoop;

namespace TextRPG.Core.EventEncounterLoop
{
    public interface IEventEncounterLoopService
    {
        void Start();
        WordSubmitResult SubmitWord(string word);
        bool UseConsumable();
        bool IsActive { get; }
    }
}
