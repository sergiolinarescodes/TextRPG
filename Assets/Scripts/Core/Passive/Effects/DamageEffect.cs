using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class DamageEffect : IPassiveEffect
    {
        public string EffectId => "damage";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0) continue;
                ctx.EntityStats.ApplyDamage(target, value, owner);
            }
        }
    }
}
