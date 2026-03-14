using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
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
        }
    }
}
