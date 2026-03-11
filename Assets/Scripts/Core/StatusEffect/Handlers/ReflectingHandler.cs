using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class ReflectingHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Reflecting;
    }
}
