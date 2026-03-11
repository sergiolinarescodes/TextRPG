using System;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Outcomes
{
    [AutoScan]
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
