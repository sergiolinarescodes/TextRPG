using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class FrozenHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Frozen;

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            if (ctx.StatusEffects.HasEffect(target, StatusEffectType.Burning))
            {
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Burning);
                ctx.StatusEffects.ApplyEffect(target, StatusEffectType.Wet, 2, target);
            }
        }
    }
}
