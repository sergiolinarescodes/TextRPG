using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class BleedingHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Bleeding;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var damage = instance.StackCount;
            ctx.EntityStats.ApplyDamage(target, damage, instance.Source);
            ctx.EventBus.Publish(new StatusEffectDamageEvent(target, StatusEffectType.Bleeding, damage));

            if (instance.WasHealedThisTurn)
            {
                instance.StackCount -= 2;
                instance.WasHealedThisTurn = false;
                if (instance.StackCount <= 0)
                    ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Bleeding);
            }
            else
            {
                instance.StackCount++;
            }
        }
    }
}
