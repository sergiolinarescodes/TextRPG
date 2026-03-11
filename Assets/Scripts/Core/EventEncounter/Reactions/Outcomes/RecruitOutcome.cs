using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class RecruitOutcome : IInteractionOutcome
    {
        public string OutcomeId => "recruit";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new RecruitEvent(context.OutcomeParam));
        }
    }
}
