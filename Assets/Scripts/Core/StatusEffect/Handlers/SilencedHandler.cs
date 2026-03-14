using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class SilencedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Silenced;
    }
}
