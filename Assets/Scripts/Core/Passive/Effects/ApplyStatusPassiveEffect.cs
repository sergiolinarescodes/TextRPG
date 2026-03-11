using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class ApplyStatusPassiveEffect : IPassiveEffect
    {
        public string EffectId => "apply_status";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            if (ctx.StatusEffects == null || effectParam == null) return;
            if (!Enum.TryParse<StatusEffectType>(effectParam, out var effectType)) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0) continue;
                ctx.StatusEffects.ApplyEffect(target, effectType, value, owner);
            }
        }
    }
}
