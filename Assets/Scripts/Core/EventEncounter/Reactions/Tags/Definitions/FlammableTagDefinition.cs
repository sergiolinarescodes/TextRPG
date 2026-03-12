using System;
using TextRPG.Core.Services;


namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    [AutoScan]
    internal sealed class FlammableTagDefinition : ITagDefinition
    {
        public string TagId => "flammable";

        public void React(TagReactionContext ctx)
        {
            if (string.Equals(ctx.ActionId, "Burn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ctx.ActionId, "Ignite", StringComparison.OrdinalIgnoreCase))
            {
                var damage = ctx.Value > 0 ? ctx.Value : 3;
                ctx.EntityStats.ApplyDamage(ctx.Target, damage, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("It catches fire!", ctx.Target));
            }
        }
    }
}
