using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class WetHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Wet;
    }
}
