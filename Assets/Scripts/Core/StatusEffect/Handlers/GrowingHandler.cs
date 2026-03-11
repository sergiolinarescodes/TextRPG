using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
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
