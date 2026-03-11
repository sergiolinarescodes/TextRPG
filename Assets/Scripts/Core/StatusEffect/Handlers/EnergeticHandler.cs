using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class EnergeticHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Energetic;

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            if (ctx.StatusEffects.HasEffect(target, StatusEffectType.Tired))
            {
                ctx.StatusEffects.DecrementStack(target, StatusEffectType.Tired);
                ctx.StatusEffects.DecrementStack(target, StatusEffectType.Tired);
                ctx.StatusEffects.ApplyEffect(target, StatusEffectType.Anxiety,
                    StatusEffectInstance.PermanentDuration, target);
            }
        }
    }
}
