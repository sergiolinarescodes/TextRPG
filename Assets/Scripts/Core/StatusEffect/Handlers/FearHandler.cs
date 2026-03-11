using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class FearHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Fear;
    }
}
