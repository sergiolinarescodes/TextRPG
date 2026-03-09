using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive.Effects
{
    internal sealed class HealEffect : IPassiveEffect
    {
        public string EffectId => "heal";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0) continue;
                ctx.EntityStats.ApplyHeal(target, value);
            }
        }
    }
}
