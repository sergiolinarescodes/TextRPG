using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class SpawnCombatOutcome : IInteractionOutcome
    {
        public string OutcomeId => "spawn_combat";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EventBus.Publish(new SpawnCombatEvent(context.OutcomeParam));
        }
    }
}
