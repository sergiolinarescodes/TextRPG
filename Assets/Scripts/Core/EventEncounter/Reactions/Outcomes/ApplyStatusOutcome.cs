using System;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    internal sealed class ApplyStatusOutcome : IInteractionOutcome
    {
        public string OutcomeId => "apply_status";

        public void Execute(InteractionOutcomeContext context)
        {
            if (context.Ctx.StatusEffects == null) return;
            if (!Enum.TryParse<StatusEffectType>(context.OutcomeParam, true, out var effectType)) return;

            context.Ctx.StatusEffects.ApplyEffect(context.Source, effectType, context.Value, context.Source);
        }
    }
}
