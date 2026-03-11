using System;
using System.Collections.Generic;
using TextRPG.Core.Services;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    [AutoScan]
    internal sealed class MercenaryTagDefinition : ITagDefinition
    {
        private const int RecruitThreshold = 10;

        private static readonly HashSet<string> ReactActions =
            new(StringComparer.OrdinalIgnoreCase) { "Pay", "Trade", "Recruit" };

        public string TagId => "mercenary";

        public void React(TagReactionContext ctx)
        {
            if (!ReactActions.Contains(ctx.ActionId)) return;

            var goldAmount = ctx.Value > 0 ? ctx.Value : 1;
            var accumulated = ctx.IncrementState("gold", goldAmount);

            if (accumulated >= RecruitThreshold)
            {
                if (!TagRecruitmentHelper.TryRecruit(ctx))
                {
                    ctx.EventBus.Publish(new InteractionMessageEvent("No room!", ctx.Target));
                    return;
                }

                ctx.EventBus.Publish(new InteractionMessageEvent(
                    "The mercenary takes your gold and joins you!", ctx.Target));
            }
            else
            {
                var remaining = RecruitThreshold - accumulated;
                ctx.EventBus.Publish(new InteractionMessageEvent(
                    $"The mercenary considers... ({remaining} more gold needed)", ctx.Target));
            }
        }
    }
}
