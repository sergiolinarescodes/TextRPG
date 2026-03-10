namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class ManaOutcome : IInteractionOutcome
    {
        public string OutcomeId => "mana";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyMana(context.Source, context.Value);
        }
    }
}
