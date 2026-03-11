using System;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    internal sealed class FlammableTagDefinition : ITagDefinition
    {
        public string TagId => "flammable";

        public void React(TagReactionContext ctx)
        {
            if (string.Equals(ctx.ActionId, "Burn", StringComparison.OrdinalIgnoreCase))
            {
                var damage = ctx.Value > 0 ? ctx.Value : 3;
                ctx.EntityStats.ApplyDamage(ctx.Target, damage, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("It catches fire!", ctx.Target));
            }
            else if (string.Equals(ctx.ActionId, "Fire", StringComparison.OrdinalIgnoreCase))
            {
                ctx.EventBus.Publish(new InteractionMessageEvent("It catches fire!", ctx.Target));
            }
        }
    }
}
