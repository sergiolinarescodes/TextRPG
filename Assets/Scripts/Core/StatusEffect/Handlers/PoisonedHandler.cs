using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class PoisonedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Poisoned;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var damage = 2 * instance.StackCount;
            ctx.EntityStats.ApplyDamage(target, damage, instance.Source);
            ctx.EventBus.Publish(new StatusEffectDamageEvent(target, StatusEffectType.Poisoned, damage));

            instance.StackCount--;
            if (instance.StackCount <= 0)
                instance.RemainingDuration = 0;
        }
    }
}
