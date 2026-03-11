using System;
using System.Collections.Generic;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    internal sealed class SocialTagDefinition : ITagDefinition
    {
        private static readonly HashSet<string> ReactActions =
            new(StringComparer.OrdinalIgnoreCase) { "Charm", "Talk", "Love" };

        public string TagId => "social";

        public void React(TagReactionContext ctx)
        {
            if (!ReactActions.Contains(ctx.ActionId)) return;

            var progress = ctx.IncrementState("progress", ctx.Value > 0 ? ctx.Value : 1);

            if (progress >= 3)
            {
                if (!TagRecruitmentHelper.TryRecruit(ctx))
                {
                    ctx.EventBus.Publish(new InteractionMessageEvent("No room!", ctx.Target));
                    return;
                }
                ctx.EventBus.Publish(new InteractionMessageEvent("It joins your side!", ctx.Target));
            }
            else
            {
                ctx.StatusEffects?.ApplyEffect(ctx.Target, StatusEffectType.Stun, 1, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("It seems friendlier...", ctx.Target));
            }
        }
    }
}
