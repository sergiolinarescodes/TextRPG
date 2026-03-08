using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class ReflectingHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Reflecting;
    }
}
