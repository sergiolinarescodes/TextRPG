using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class RewardOutcome : IInteractionOutcome
    {
        public string OutcomeId => "reward";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new RewardGrantedEvent(context.OutcomeParam, context.Value));
            context.Ctx.EventBus.Publish(new InteractionMessageEvent(
                $"+{context.Value} {context.OutcomeParam}", context.Target));
        }
    }
}
