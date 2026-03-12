using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class GoldEffect : IPassiveEffect
    {
        public string EffectId => "gold";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            if (ctx.ResourceService == null) return;
            ctx.ResourceService.Add(ResourceIds.Gold, value);
        }
    }
}
