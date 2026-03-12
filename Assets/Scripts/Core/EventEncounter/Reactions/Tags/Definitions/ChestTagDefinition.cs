using System;
using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    [AutoScan]
    internal sealed class ChestTagDefinition : ITagDefinition
    {
        public string TagId => "chest";

        public void React(TagReactionContext ctx)
        {
            if (string.Equals(ctx.ActionId, "Open", StringComparison.OrdinalIgnoreCase))
            {
                ctx.EventBus.Publish(new ChestOpenedEvent(ctx.Target, ctx.Source));
            }
        }
    }
}
