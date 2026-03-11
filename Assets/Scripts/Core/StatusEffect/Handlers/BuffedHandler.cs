using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class BuffedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Buffed;
    }
}
