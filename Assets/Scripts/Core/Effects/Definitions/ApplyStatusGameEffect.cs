using System;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Services;

namespace TextRPG.Core.Effects.Definitions
{
    [AutoScan]
    internal sealed class ApplyStatusGameEffect : IGameEffect
    {
        public string EffectId => "apply_status";

        public void Execute(EffectContext context)
        {
            var statusEffects = context.Services.StatusEffects;
            if (statusEffects == null || context.Param == null) return;
            if (!Enum.TryParse<StatusEffectType>(context.Param, true, out var effectType)) return;

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var stats = context.Services.EntityStats;
                if (!stats.HasEntity(target) || stats.GetCurrentHealth(target) <= 0) continue;
                statusEffects.ApplyEffect(target, effectType, context.Value, context.Source);
            }
        }
    }
}
