namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class ConsumeOutcome : IInteractionOutcome
    {
        public string OutcomeId => "consume";

        public void Execute(InteractionOutcomeContext context)
        {
            var hp = context.Ctx.EntityStats.GetCurrentHealth(context.Target);
            if (hp > 0)
                context.Ctx.EntityStats.ApplyDamage(context.Target, hp, context.Source);
        }
    }
}
