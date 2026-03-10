namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class SpawnCombatOutcome : IInteractionOutcome
    {
        public string OutcomeId => "spawn_combat";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new SpawnCombatEvent(context.OutcomeParam));
        }
    }
}
