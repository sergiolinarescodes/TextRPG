using System;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    internal sealed class MeltableTagDefinition : ITagDefinition
    {
        public string TagId => "meltable";

        public void React(TagReactionContext ctx)
        {
            if (string.Equals(ctx.ActionId, "Melt", StringComparison.OrdinalIgnoreCase))
            {
                var hp = ctx.EntityStats.GetStat(ctx.Target, EntityStats.StatType.Health);
                ctx.EntityStats.ApplyDamage(ctx.Target, hp, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("It melts open!", ctx.Target));
            }
        }
    }
}
