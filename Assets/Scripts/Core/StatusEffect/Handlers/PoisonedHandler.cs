using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class PoisonedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Poisoned;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var definition = StatusEffectDefinitions.Get(StatusEffectType.Poisoned);
            if (definition.DamagePerTick.HasValue)
            {
                var damage = definition.DamagePerTick.Value * instance.StackCount;
                ctx.EntityStats.ApplyDamage(target, damage);
                ctx.EventBus.Publish(new StatusEffectDamageEvent(target, StatusEffectType.Poisoned, damage));
            }
        }
    }
}
