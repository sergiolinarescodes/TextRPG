using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    public interface IStatusEffectHandler
    {
        StatusEffectType EffectType { get; }
        void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx);
        void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx);
        void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx);
        void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx);
    }
}
