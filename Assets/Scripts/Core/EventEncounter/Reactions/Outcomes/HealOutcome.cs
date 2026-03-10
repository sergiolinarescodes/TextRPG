namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class HealOutcome : IInteractionOutcome
    {
        public string OutcomeId => "heal";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyHeal(context.Source, context.Value);
        }
    }
}
