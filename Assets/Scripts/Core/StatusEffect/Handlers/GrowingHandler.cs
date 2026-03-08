using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class GrowingHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Growing;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var healAmount = instance.StackCount;
            if (ctx.StatusEffects.HasEffect(target, StatusEffectType.Wet))
                healAmount += 2;
            ctx.EntityStats.ApplyHeal(target, healAmount);
        }
    }
}
