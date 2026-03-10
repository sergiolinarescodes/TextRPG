namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class LeaveOutcome : IInteractionOutcome
    {
        public string OutcomeId => "leave";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EncounterService?.EndEncounter();
        }
    }
}
