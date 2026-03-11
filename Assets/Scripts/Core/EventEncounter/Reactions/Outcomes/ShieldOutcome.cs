using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class ShieldOutcome : IInteractionOutcome
    {
        public string OutcomeId => "shield";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyShield(context.Source, context.Value);
        }
    }
}
