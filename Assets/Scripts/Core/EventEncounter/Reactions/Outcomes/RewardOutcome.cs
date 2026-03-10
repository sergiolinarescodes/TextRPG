namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class RewardOutcome : IInteractionOutcome
    {
        public string OutcomeId => "reward";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new RewardGrantedEvent(context.OutcomeParam, context.Value));
        }
    }
}
