using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class HealOutcome : IInteractionOutcome
    {
        public string OutcomeId => "heal";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyHeal(context.Source, context.Value);
        }
    }
}
