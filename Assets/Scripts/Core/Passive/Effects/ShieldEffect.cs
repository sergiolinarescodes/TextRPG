using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive.Effects
{
    internal sealed class ShieldEffect : IPassiveEffect
    {
        public string EffectId => "shield";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0) continue;
                ctx.EntityStats.ApplyShield(target, value);
            }
        }
    }
}
