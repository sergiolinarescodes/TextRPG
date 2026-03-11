using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class SlowedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Slowed;
    }
}
