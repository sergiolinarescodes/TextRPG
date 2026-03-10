namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class DamageOutcome : IInteractionOutcome
    {
        public string OutcomeId => "damage";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyDamage(context.Source, context.Value, context.Target);
        }
    }
}
