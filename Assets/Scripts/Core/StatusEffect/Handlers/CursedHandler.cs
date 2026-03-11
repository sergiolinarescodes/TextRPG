using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class CursedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Cursed;
    }
}
