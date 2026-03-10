namespace TextRPG.Core.EventEncounter.Reactions
{
    public interface IInteractionOutcome
    {
        string OutcomeId { get; }
        void Execute(InteractionOutcomeContext context);
    }
}
