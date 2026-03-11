using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class ManaEffect : IPassiveEffect
    {
        public string EffectId => "mana";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target)) continue;
                ctx.EntityStats.ApplyMana(target, value);
            }
        }
    }
}
