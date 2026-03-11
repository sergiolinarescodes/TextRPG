using System;
using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    [AutoScan]
    internal sealed class ConductiveTagDefinition : ITagDefinition
    {
        public string TagId => "conductive";

        public void React(TagReactionContext ctx)
        {
            if (string.Equals(ctx.ActionId, "Shock", StringComparison.OrdinalIgnoreCase))
            {
                var damage = ctx.Value > 0 ? ctx.Value : 2;
                ctx.EntityStats.ApplyDamage(ctx.Target, damage, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("Electricity surges through it!", ctx.Target));
            }
        }
    }
}
