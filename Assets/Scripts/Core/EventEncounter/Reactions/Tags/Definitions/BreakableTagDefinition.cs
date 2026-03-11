using System;
using System.Collections.Generic;

namespace TextRPG.Core.EventEncounter.Reactions.Tags.Definitions
{
    internal sealed class BreakableTagDefinition : ITagDefinition
    {
        private static readonly HashSet<string> ReactActions =
            new(StringComparer.OrdinalIgnoreCase) { "Damage", "Smash" };

        public string TagId => "breakable";

        public void React(TagReactionContext ctx)
        {
            if (!ReactActions.Contains(ctx.ActionId)) return;

            var hits = ctx.IncrementState("hits");
            if (hits >= 2)
            {
                var hp = ctx.EntityStats.GetStat(ctx.Target, EntityStats.StatType.Health);
                ctx.EntityStats.ApplyDamage(ctx.Target, hp, ctx.Source);
                ctx.EventBus.Publish(new InteractionMessageEvent("It shatters!", ctx.Target));
            }
            else
            {
                ctx.EventBus.Publish(new InteractionMessageEvent("It cracks under the force!", ctx.Target));
            }
        }
    }
}
