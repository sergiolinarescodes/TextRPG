using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class MessageOutcome : IInteractionOutcome
    {
        public string OutcomeId => "message";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new InteractionMessageEvent(context.OutcomeParam, context.Target));
        }
    }
}
