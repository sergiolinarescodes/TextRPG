using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    public abstract class BaseStatusEffectHandler : IStatusEffectHandler
    {
        public abstract StatusEffectType EffectType { get; }
        public virtual void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx) { }
        public virtual void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx) { }
        public virtual void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx) { }
        public virtual void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx) { }
    }
}
