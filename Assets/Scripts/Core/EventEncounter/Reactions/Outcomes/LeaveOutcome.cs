using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class LeaveOutcome : IInteractionOutcome
    {
        public string OutcomeId => "leave";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EncounterService?.EndEncounter();
        }
    }
}
