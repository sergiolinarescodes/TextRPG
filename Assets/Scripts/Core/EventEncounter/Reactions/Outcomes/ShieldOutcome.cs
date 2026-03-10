using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class ShieldOutcome : IInteractionOutcome
    {
        public string OutcomeId => "shield";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyShield(context.Source, context.Value);
        }
    }
}
