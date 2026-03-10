namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class TransitionOutcome : IInteractionOutcome
    {
        public string OutcomeId => "transition";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new EventEncounterTransitionEvent(context.OutcomeParam));
        }
    }
}
