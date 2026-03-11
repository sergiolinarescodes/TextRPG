using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
    internal sealed class DamageInteractableOutcome : IInteractionOutcome
    {
        public string OutcomeId => "damage_target";

        public void Execute(InteractionOutcomeContext context)
        {
            context.Ctx.EntityStats.ApplyDamage(context.Target, context.Value, context.Source);
        }
    }
}
