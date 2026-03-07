using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class ConcussionHandler : BaseStatusEffectHandler
    {
        private static readonly Random Rng = new();

        public override StatusEffectType EffectType => StatusEffectType.Concussion;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            if (Rng.Next(2) == 0)
            {
                ctx.EntityStats.ApplyDamage(target, 1);
                ctx.StatusEffects.ApplyEffect(target, StatusEffectType.Stun, 1, instance.Source);
            }

            instance.StackCount = Math.Max(0, instance.StackCount - 1);
        }
    }
}
