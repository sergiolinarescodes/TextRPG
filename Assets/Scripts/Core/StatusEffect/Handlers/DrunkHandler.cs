using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class DrunkHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Drunk;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            instance.StackCount--;
            if (instance.StackCount <= 0)
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Drunk);
        }
    }
}
